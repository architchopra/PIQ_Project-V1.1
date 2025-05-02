namespace PIQ_Project
    {
    class Program
        {
        static void Main ( string [ ] args )
            {
            try
                {
                // Add your app secret Key here . The app_key looks like : 00000000-0000-0000-0000-000000000000:00000000-0000-0000-0000-000000000000
                string appSecretKey = "1bfa74ea-189e-0c4b-448b-652e64b74405:b20bc087-3b20-e525-bbd4-47cefe302c89";

                // Set the environment the app needs to run in here
                tt_net_sdk.ServiceEnvironment environment = tt_net_sdk.ServiceEnvironment.ProdLive;

                // Select the mode in which you wish to run -- Client (outside the TT datacenter)  
                //                                          or Server (on a dedicated machine inside TT datacenter)
                tt_net_sdk.TTAPIOptions.SDKMode sdkMode = tt_net_sdk.TTAPIOptions.SDKMode.Client;
                tt_net_sdk.TTAPIOptions apiConfig = new tt_net_sdk.TTAPIOptions(
                        sdkMode,
                        environment,
                        appSecretKey,
                        5000);

                // set any other SDK options you need configured

                apiConfig. EnableEstimatedPositionInQueue = true;
                // Start the TT API on the same thread
                TTNetApiFunctions tf = new TTNetApiFunctions();

                Thread workerThread = new Thread(() => tf.Start(apiConfig));
                workerThread. Name = "TT NET SDK Thread";
                workerThread. Start ( );

                while ( true )
                    {
                    string input = System.Console.ReadLine();
                    if ( input == "q" )
                        break;
                    }
                tf. Dispose ( );
                }
            catch ( Exception e )
                {
                Console. WriteLine ( e. Message + "\n" + e. StackTrace );
                }

            }
        }
    }