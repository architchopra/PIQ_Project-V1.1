using System. Collections. Concurrent;

namespace PIQ_Project
    {
    public class SlidingWindow
        {
        private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _windowSize_short = TimeSpan.FromSeconds(10);
        private readonly ConcurrentDictionary<string, List<(DateTime timestamp, int quantity)>> _data = new();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> eventMap=new ConcurrentDictionary<string, ConcurrentQueue<DateTime>>();
        // Method to add data (string, quantity) into the dictionary
        public void Add ( string key, int quantity )
            {
            var now = DateTime.Now;

            _data. AddOrUpdate ( key,
                new List<(DateTime timestamp, int quantity)> { (now, quantity) },
                ( existingKey, existingList ) =>
                {
                    lock ( existingList )
                        {
                        // Cleanup old entries
                        existingList. RemoveAll ( entry => ( now - entry. timestamp ) > _windowSize );
                        existingList. Add ( (now, quantity) );
                        return existingList;
                        }
                } );
            }

        // Method to retrieve sum of quantities for the last 30 minutes
        public int GetSumLast30Minutes ( string key )
            {
            if ( !_data. TryGetValue ( key, out var list ) ) return 0;

            var now = DateTime.Now;

            lock ( list )
                {
                // Cleanup old entries and sum up valid ones
                list. RemoveAll ( entry => ( now - entry. timestamp ) > _windowSize );
                return list. Sum ( entry => entry. quantity );
                }
            }
        public int AddEvent ( string key, DateTime eventTime )
            {
            if ( string. IsNullOrEmpty ( key ) ) return 0;

            // Get or add a new queue for the key
            var queue = eventMap.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());
            queue. Enqueue ( eventTime );

            // Remove events outside the window
            RemoveOldEvents ( queue, eventTime );
            return queue. Count;
            }
        private void RemoveOldEvents ( ConcurrentQueue<DateTime> queue, DateTime currentEventTime )
            {
            var cutoff = currentEventTime - _windowSize_short; // Calculate cutoff based on the current event time
            while ( queue. TryPeek ( out var timestamp ) && timestamp < cutoff )
                {
                queue. TryDequeue ( out _ );
                }
            }
        }
    }
