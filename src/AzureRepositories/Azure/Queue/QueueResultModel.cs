namespace AzureRepositories.Azure.Queue
{
    public class QueueRequestModel<T>
    {
        public T Data { get; set; }
    }

    public class QueueResultModel
    {
        public class ErrorModel
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }

        public ErrorModel Error { get; set; }
    }

    public class QueueResultModel<T> : QueueResultModel
    {
        public T Result { get; set; }
    }
}
