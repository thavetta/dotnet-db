namespace Bookings.Infrastructure;

public static class MyDbFunctions
{
    // Body se v SQL nepoužije; je zde kvůli volání v LINQ
    public static int Nights(DateTime from, DateTime to) => (to - from).Days;
}
