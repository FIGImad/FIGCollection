namespace FIGAutoTraderAdminSvc.Models
{

    public class AdminResponesDto
    {
        public const int SUCCESS = 0;
        public const int USER_NOT_FOUND_IN_DATABASE = 1;
        public const int DATABASE_MISSING = 2;
        public const int FAILED_TO_FIX_ISSUE = 3;
        public const int UNABLE_TO_RETRIEVE_LIST = 4;

        public string Id{ get; set; } = "";
        public int Status { get; set; } = 0;
        public string Message { get; set; } = "";
        public Object? Obj { get; set; }

        public AdminResponesDto() { }
        public AdminResponesDto(AdminResponesDto data)
        {
            Id = data.Id;
            Status = data.Status;
            Message = data.Message;
            Obj = data.Obj;
        }
    }
}
