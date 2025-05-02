using Serilog;
using Serilog. Events;

namespace PIQ_Project
    {
    public static class Logger
        {
        private static readonly Serilog.ILogger _mainLogger;
        private static readonly Serilog.ILogger _infoLogger;

        static Logger ( )
            {
            _mainLogger = new LoggerConfiguration ( )
                . MinimumLevel. Verbose ( )
                //.WriteTo.Console()
                . WriteTo. File (
                    "C:\\tt\\APC\\PIQ\\logs\\PIQ DATA LOGS\\Testlogs.txt",
                    rollingInterval: RollingInterval. Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fffffff} [{Level:u3}] {Message:lj}{NewLine}{Exception}" )
                . CreateLogger ( );

            _infoLogger = new LoggerConfiguration ( )
                . MinimumLevel. Information ( )
                . WriteTo. File (
                    "C:\\tt\\APC\\PIQ\\logs\\PIQ DATA LOGS\\InformationLogs.txt",
                    rollingInterval: RollingInterval. Day,
                    restrictedToMinimumLevel: LogEventLevel. Information,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fffffff} [INFO] {Message:lj}{NewLine}{Exception}" )
                . CreateLogger ( );
            }

        public static Task WriteLogAsync ( LogEventLevel level, string messageTemplate, params object [ ] propertyValues )
            {
            return Task. Run ( ( ) =>
            {

                if ( level == LogEventLevel. Information )
                    {
                    _infoLogger. Write ( level, messageTemplate, propertyValues );
                    }
                else
                    {
                    _mainLogger. Write ( level, messageTemplate, propertyValues );
                    }
            } );
            }

        public static Task DebugAsync ( string message, params object [ ] propertyValues ) => WriteLogAsync ( LogEventLevel. Debug, message, propertyValues );
        public static Task InformationAsync ( string message, params object [ ] propertyValues ) => WriteLogAsync ( LogEventLevel. Information, message, propertyValues );
        public static Task WarningAsync ( string message, params object [ ] propertyValues ) => WriteLogAsync ( LogEventLevel. Warning, message, propertyValues );
        public static Task ErrorAsync ( string message, params object [ ] propertyValues ) => WriteLogAsync ( LogEventLevel. Error, message, propertyValues );
        public static Task FatalAsync ( string message, params object [ ] propertyValues ) => WriteLogAsync ( LogEventLevel. Fatal, message, propertyValues );

        public static void CloseAndFlush ( )
            {
            Log. CloseAndFlush ( );
            }
        }
    }
