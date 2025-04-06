using System.Runtime.InteropServices;
using Terraria;

class Program
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    static void Main()
    {
        #if !DEBUG
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        #endif
        GameWindow window = new();
    }
}