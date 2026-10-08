namespace MvcHybridBackChannelTwo.BackChannelLogout;

public class BackchannelLogoutSession
{
    public string? Sub { get; set; }
    public string? Sid { get; set; }

    public bool IsMatch(string? sub, string? sid)
    {
        return (Sid == sid && Sub == sub) ||
               (Sid == sid && Sub == null) ||
               (Sid == null && Sub == sub);
    }
}