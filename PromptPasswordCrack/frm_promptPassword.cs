using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;


namespace PromptPasswordCrack
{
    public partial class frm_promptPassword : Form
    {
        private IKeyboardMouseEvents m_GlobalHook;
        private Boolean hookSubscribe = false;

        //This variable holds the filepath/name of the file we're going to use for the brute force attack.
        private String fileName;

        //The below variables hold information that we will use in the status label.
        private int totalRecords;
        private int currentRecord = 0;
        private DateTime startDateOfBruteForce;
        private String status;

        //The below variable holds the 
        private String[] dictionary;

        //This where we create the variable 
        Thread thread_brute;

        //This holds the previous window's hWnd
        IntPtr intPtr_previous;

        //This records the number of times we've had to pause between password enters
        int int_intPtrPreviousCount = 0;

        public frm_promptPassword()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void Subscribe()
        {
            // Note: for the application hook, use the Hook.AppEvents() instead
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;
            //m_GlobalHook.KeyPress += GlobalHookKeyPress;
        }

        private void GlobalHookKeyPress(object sender, KeyPressEventArgs e)
        {
            Console.WriteLine("KeyPress: \t{0}", e.KeyChar);
        }

        private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            //Set hWnd Label to the hWnd of the control clicked
            IntPtr hWnd = Win32.GetWindowHandleFromPoint(MousePosition.X, MousePosition.Y);
            lbl_hWnd.Text = hWnd.ToString();
            //Set txt_PasswordBoxTitle to the title of the password box window.
            txt_PasswordBoxTitle.Text = Win32.GetWindowTitle(hWnd);
            // Set the Textbox to the title of the parent window.
            txt_ParentTitle.Text = Win32.GetWindowTitle(Win32.getAbsoluteParent(hWnd));
            //Unsubscribe / Deactivate the system hook
            Unsubscribe();
            hookSubscribe = false;
        }

        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;
            m_GlobalHook.KeyPress -= GlobalHookKeyPress;

            //It is recommened to dispose it
            m_GlobalHook.Dispose();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            //Subscribe to Hooks
            if (hookSubscribe == false)
            {
                Subscribe();
                hookSubscribe = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IntPtr parent = Win32.FindWindow(null,"");
            IntPtr child = Win32.FindWindow(IntPtr.Zero, ptr => Win32.GetWindowTitle(ptr) == txt_PasswordBoxTitle.Text);
            //Win32.FindWindow(child, ptr => Win32.GetWindowTitle(ptr) == "&OK");
            IntPtr edit = Win32.FindWindowEx(child, IntPtr.Zero, "Edit", null);
            IntPtr button = Win32.FindWindow(child, ptr => Win32.GetWindowTitle(ptr) == txt_PasswordBoxOkButton.Text);
            String stringParent = parent.ToString();
            String stringChild = child.ToString();
            String stringButton = button.ToString();
            String message = "Parent: " + stringParent + "\n" + "Child: " + stringChild + "\n" + Win32.GetWindowTitle(child) + "\n" + "Edit: " + edit + "\n" + stringButton;
            //MessageBox.Show(message);
            //Click the enter button
            Win32.SetText(edit, txt_password.Text);
            Win32.clickButton(button);

        }

        private void tmr_status_Tick(object sender, EventArgs e)
        {
            TimeSpan timeElapsed;
            double secondsRemaining;
            TimeSpan remainingTime;
            //Formats the total passwords amount with commas for thousands.
            String total = totalRecords.ToString("N0");
            String password = ""; 
            try
            {
                //Figure out what the timelapse has been, we need to get the distance between thestartTime and the currentTime.
                //Then we divide those seconds by the number of records so far and then multiply that by the records left to give us the time remaining.
                timeElapsed = DateTime.Now.Subtract(startDateOfBruteForce);
                secondsRemaining = (timeElapsed.TotalSeconds / currentRecord) * (totalRecords - currentRecord);
                remainingTime = TimeSpan.FromSeconds(secondsRemaining);
                password = (String)dictionary.GetValue(currentRecord);
            }
            catch
            {
                //Let's assume we're testing, or no file is selected yet.
                remainingTime = TimeSpan.FromSeconds(0);
            }
            //This keeps the status label updated with what is going on.
            status = $"Total Records: {total}     |     Current Record: {currentRecord.ToString("N0")}  /  {password}";
            lbl_status.Text = status;
            status = $"Time Remaining: {remainingTime.Days}d {remainingTime.Hours}h {remainingTime.Minutes}m {remainingTime.Seconds}s";
            lbl_status2.Text = status;
            //Updates the progress bar
            pb_dictionary.Value = currentRecord;
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            //We're going to create a open file dialogue here and use it to set the fileName variable.
            OpenFileDialog openFile;
            openFile = new OpenFileDialog();
            openFile.InitialDirectory = Application.StartupPath;
            openFile.Title = "Choose a dictionary file";
            openFile.ShowDialog();
            fileName = openFile.FileName;
            loadDictionary(fileName);
        }

        private void loadDictionary(String filename)
        {
            //This loads the dictionary file into memory, hopefully without cracking.
            dictionary = System.IO.File.ReadAllLines(filename);
            totalRecords = dictionary.Length;
            pb_dictionary.Maximum = totalRecords;
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            if (thread_brute == null)
            {
                //Set DateTime of BruteForce
                startDateOfBruteForce = DateTime.Now;
                //This where we start the brute force.
                thread_brute = new Thread(new ThreadStart(brute));
                thread_brute.Start();
            } else
            {
                if (thread_brute.ThreadState == ThreadState.Suspended)
                {
                    thread_brute.Resume();
                }
            }
        }

        private void brute()
        {

            //Get the next password from the dictionary array
            String password; 

            for (int i = 0; i < totalRecords; i++)
            {
                //This is  poor hack, but if the intPtr of the child window is the same as the previous we jump back to here
                jumpIn:

                password = (String)dictionary.GetValue(i);

                IntPtr parent = Win32.FindWindow(null, "");
                //IntPtr child = Win32.FindWindow(IntPtr.Zero, ptr => Win32.GetWindowTitle(ptr) == txt_PasswordBoxTitle.Text);
                IntPtr child = Win32.FindWindow(null, txt_PasswordBoxTitle.Text);

                if (child == intPtr_previous)
                {
                    //The hWnd of child was the same as the intPtr_previous, so this is the same window, apparently the password hasn't been passed yet.
                    //We will pause for X amount of time.
                    switch (int_intPtrPreviousCount)
                    {
                        case 0:
                            System.Threading.Thread.Sleep(50);
                            break;
                        case 1:
                            System.Threading.Thread.Sleep(250);
                            break;
                        case 2:
                            System.Threading.Thread.Sleep(500);
                            break;
                        case 3:
                            System.Threading.Thread.Sleep(1000);
                            break;
                    }

                    //Now we jump to the begining of the FOR loop w/o incrementing the count variable
                    int_intPtrPreviousCount++;
                    goto jumpIn;
                } else
                {
                    //Let's make sure to reset int_intPtrPreviousCount to 0
                    int_intPtrPreviousCount = 0;

                    IntPtr edit = Win32.FindWindowEx(child, IntPtr.Zero, "Edit", null);
                    IntPtr button = Win32.FindWindow(child, ptr => Win32.GetWindowTitle(ptr) == txt_PasswordBoxOkButton.Text);

                    //Set old hWnd
                    intPtr_previous = child;

                    //Set Password
                    Win32.SetText(edit, password);
                    //Click the enter button
                    Win32.clickButton(button);

                    //Let's increment this so the display shows the current record.
                    currentRecord++;
                }
            }
        }

        //Once clicked this should 
        private void Button5_Click(object sender, EventArgs e)
        {
            if (thread_brute != null)
            {
                if (thread_brute.ThreadState != ThreadState.Suspended && thread_brute.ThreadState != ThreadState.Stopped)
                {
                    thread_brute.Suspend();
                }
            }
        }
    }
}
