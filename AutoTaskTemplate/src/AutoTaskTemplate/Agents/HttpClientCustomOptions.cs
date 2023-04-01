
namespace AutoTaskTemplate.Agents
{
    public class HttpClientCustomOptions
    {
        public int RandomDelaySecondsBetweenCalls { get; set; } = 5;

        public string WebProxy { get; set; }
    }
}
