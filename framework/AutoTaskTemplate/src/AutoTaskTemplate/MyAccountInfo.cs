using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Ray.Infrastructure.AutoTask;

namespace AutoTaskTemplate;

public class MyAccountInfo : TargetAccountInfo
{
    public MyAccountInfo() { }

    public MyAccountInfo(string userName, string pwd) : base(userName, pwd)
    {
    }

    private string _nickName;

    public string NickName
    {
        get => string.IsNullOrWhiteSpace(_nickName)
            ? GetNickName(this.UserName)
            : _nickName;
        set => _nickName = value;
    }

    private string GetNickName(string userName)
    {
        var uname = string.IsNullOrWhiteSpace(userName) ? "" : userName.Split("@").ToList().First();
        return string.IsNullOrWhiteSpace(uname) ? GetKeyValueFromStates() : uname;
    }

    public string States { get; set; }

    public string KeyNameFromStates => "DedeUserID";

    public string GetKeyValueFromStates()
    {
        if (string.IsNullOrWhiteSpace(States)) return "";

        dynamic stateObj = JsonConvert.DeserializeObject(States);
        var ckList = (JArray)stateObj["cookies"];
        var ck = ckList.FirstOrDefault(x => x["name"].ToString() == KeyNameFromStates);
        var uid = ck["value"].ToString();
        return uid;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        MyAccountInfo other = (MyAccountInfo)obj;
        return this.GetKeyValueFromStates() == other.GetKeyValueFromStates();
    }
}