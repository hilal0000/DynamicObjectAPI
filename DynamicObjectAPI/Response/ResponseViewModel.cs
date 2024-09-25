namespace DynamicObjectAPI.Response
{

        public class ResponseViewModel<T>
    {
            public bool IsSuccess { get; set; }
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
            public T Data { get; set; }

    }
    
}
