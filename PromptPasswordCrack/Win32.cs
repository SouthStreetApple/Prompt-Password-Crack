using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PromptPasswordCrack
{
    public class Win32
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <remarks>https://stackoverflow.com/questions/4604023/unable-to-read-another-applications-caption</remarks>
        public static string GetWindowTitle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                throw new ArgumentNullException("hwnd");
            int length = Win32.SendMessageGetTextLength(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
            if (length > 0 && length < int.MaxValue)
            {
                length++; // room for EOS terminator
                StringBuilder sb = new StringBuilder(length);
                Win32.SendMessageGetText(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
                return sb.ToString();
            }
            return String.Empty;
        }

        public static IntPtr GetWindowHandleFromPoint(int x, int y)
        {
            var point = new Point(x, y);
            return Win32.WindowFromPoint(point);
        }

        public static void SetText(IntPtr hWnd, String text_to_Send)
        {
            SendMessage(hWnd, 0x000C, 0, text_to_Send);
        }

        public static void clickButton(IntPtr hWnd)
        {
            SendMessage(hWnd, BN_CLICKED, 0, "0");
        }

        public static IntPtr getAbsoluteParent(IntPtr hWnd)
        {
            IntPtr intPtr = hWnd;
            int parent_ptr;

            do
            {
                intPtr = GetParent(hWnd);
                parent_ptr = (int) GetParent(intPtr);
                hWnd = intPtr;
            } while (parent_ptr != 0);
            //When this exits the loop I know that intPtr is the last hWnd before the 0, so it's the absolute parent.               
            return intPtr;
        }


        const int WM_GETTEXT = 0x000D;
        const int WM_GETTEXTLENGTH = 0x000E;
        private const int BN_CLICKED = 245;

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point p);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessageGetTextLength(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageGetText(IntPtr hWnd, int msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);
        /// <summary>
        /// Find a child window that matches a set of conditions specified as a Predicate that receives hWnd.  Returns IntPtr.Zero
        /// if the target window not found.  Typical search criteria would be some combination of window attributes such as
        /// ClassName, Title, etc., all of which can be obtained using API functions you will find on pinvoke.net
        /// </summary>
        /// <remarks>
        ///     <para>Example: Find a window with specific title (use Regex.IsMatch for more sophisticated search)</para>
        ///     <code lang="C#"><![CDATA[var foundHandle = Win32.FindWindow(IntPtr.Zero, ptr => Win32.GetWindowText(ptr) == "Dashboard");]]></code>
        /// </remarks>
        /// <param name="parentHandle">Handle to window at the start of the chain.  Passing IntPtr.Zero gives you the top level
        /// window for the current process.  To get windows for other processes, do something similar for the FindWindow
        /// API.</param>
        /// <param name="target">Predicate that takes an hWnd as an IntPtr parameter, and returns True if the window matches.  The
        /// first match is returned, and no further windows are scanned.</param>
        /// <returns> hWnd of the first found window, or IntPtr.Zero on failure </returns>
        public static IntPtr FindWindow(IntPtr parentHandle, Predicate<IntPtr> target)
        {
            var result = IntPtr.Zero;
            EnumChildWindows(parentHandle, (hwnd, param) => {
                if (target(hwnd))
                {
                    result = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }
    }
}
