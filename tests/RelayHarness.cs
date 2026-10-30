using System;
using ProGo;

internal static class RelayHarness
{
    private static int Main(string[] args)
    {
        try
        {
            using (var relay = new Ikev2RelayService())
            {
                relay.StartAsync("127.0.0.1", Int32.Parse(args[0]), Int32.Parse(args[1]),
                    Int32.Parse(args[2]), Int32.Parse(args[3])).GetAwaiter().GetResult();
                Console.WriteLine("READY");
                string line;
                while ((line = Console.ReadLine()) != null && line != "quit")
                {
                    if (line == "stop-owned") {
                        relay.Stop(); Console.WriteLine("STOPPED " + relay.PendingStopCount); continue;
                    }
                    if (line == "stop") relay.Stop();
                    Console.WriteLine(relay.Received + " " + relay.Sent + " " + relay.Returned + " " + relay.Dropped);
                }
                // This console owns its subprocess lifetime; Dispose itself is
                // deliberately non-blocking for UI callers.
                relay.StopAsync().GetAwaiter().GetResult();
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.GetType().Name); return 1; }
    }
}
