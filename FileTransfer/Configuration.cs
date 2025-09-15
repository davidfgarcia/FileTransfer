namespace FileTransferAssignment
{
    public static class Configuration
    {
        public const int NUMBER_OF_SERVERS = 10;
        public const int SERVER_START_RETRIES = 3;
        public const int DATA_TRANSFER_RETRIES = 10;
        public const int MAX_CONCURRENT_INCOMING = 5;
        public const int MAX_CONCURRENT_OUTGOING = 5;
        public const int TIMEOUT_MILLISECONDS = 5 * 60 * 1000; // five minutes
        public const int HEARTBEAT_FREQUENCY = 1000; // check status every second

        
        public static bool CLEAN_UP_PARTIAL_FILES = false;
    }
}