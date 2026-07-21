namespace FIGCommon.Models.FIGController
{
    public class ControllerRouteRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string Route { get; set; } = string.Empty;
        public string Method { get; set; } = HttpMethod.Get.Method;
        public int DestinationRole { get; set; } = 0;
        public string DestinationServiceId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string? Load { get; set; }

        public int Attempt { get; set; } = 1;
    }
}
