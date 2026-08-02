namespace MoraTuk.Mobile.Services;

public static class SessionService
{
    public static async Task SaveSession(
        string token,
        int userId,
        string role)
    {
        await SecureStorage.SetAsync("token", token);
        await SecureStorage.SetAsync("userId", userId.ToString());
        await SecureStorage.SetAsync("role", role);
    }


    public static async Task<string?> GetToken()
    {
        return await SecureStorage.GetAsync("token");
    }


    public static async Task<string?> GetRole()
    {
        return await SecureStorage.GetAsync("role");
    }


    public static async Task Logout()
    {
        SecureStorage.Remove("token");
        SecureStorage.Remove("userId");
        SecureStorage.Remove("role");
    }
}