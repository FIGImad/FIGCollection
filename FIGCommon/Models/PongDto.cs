namespace FIGCommon.Models
{
    public class PongDto
    {
        public PongDto() {
            Date = DateTime.Now;
            Pong = "Pong"; 
        }
        public DateTime Date { get; set; }
        public string Pong { get; set; }
    }
}