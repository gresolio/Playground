using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ConsoleSnake
{
    public class Program
    {
        const int SCREEN_WIDTH = 120;
        const int SCREEN_HEIGHT = 50;

        const int MIN_FRAME_DELAY = 5;
        const int MAX_FRAME_DELAY = 100;
        const int MAX_INPUT_HISTORY = 2;

        const int DIRECTION_UP = 0;
        const int DIRECTION_RIGHT = 1;
        const int DIRECTION_DOWN = 2;
        const int DIRECTION_LEFT = 3;

        struct SnakeSegment
        {
            public int X;
            public int Y;
        }

        static char[] screen = new char[SCREEN_WIDTH * SCREEN_HEIGHT];
        static IntPtr hConsole = INVALID_HANDLE_VALUE;
        static Random rnd = new Random();

        static void Main(string[] args)
        {
            InitConsole();
            StartGameLoop();
        }

        static void InitConsole()
        {
            hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
            if (hConsole == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("Error: cannot get Console handle.");
                Console.ReadKey();
                Environment.Exit(-1);
            }

            Console.SetWindowSize(SCREEN_WIDTH, SCREEN_HEIGHT);
            Console.SetBufferSize(SCREEN_WIDTH, SCREEN_HEIGHT);
            Console.CursorVisible = false;
            SetConsoleFont(hConsole, "Lucida Console");
        }

        static void SetConsoleFont(IntPtr hnd, string fontName)
        {
            if (hnd != INVALID_HANDLE_VALUE && !string.IsNullOrEmpty(fontName))
            {
                CONSOLE_FONT_INFO_EX info = new CONSOLE_FONT_INFO_EX();
                info.cbSize = (uint)Marshal.SizeOf(info);
                bool tt = false;
                if (GetCurrentConsoleFontEx(hnd, false, ref info))
                {
                    tt = (info.FontFamily & TMPF_TRUETYPE) == TMPF_TRUETYPE;
                    if (tt)
                    {
                        Console.WriteLine("The console already is using a TrueType font.");
                        //return;
                    }

                    CONSOLE_FONT_INFO_EX newInfo = new CONSOLE_FONT_INFO_EX();
                    newInfo.cbSize = (uint)Marshal.SizeOf(newInfo);
                    newInfo.FontFamily = TMPF_TRUETYPE;
                    newInfo.FaceName = fontName;

                    // Get some settings from current font.
                    newInfo.dwFontSize = new COORD(info.dwFontSize.X, info.dwFontSize.Y);
                    newInfo.FontWeight = info.FontWeight;

                    SetCurrentConsoleFontEx(hnd, false, ref newInfo);
                }
            }
        }

        static void StartGameLoop()
        {
            while (true)
            {
                // Reset game state
                int score = 0;
                int speed = 20;
                int foodX = 30;
                int foodY = 15;
                bool dead = false;
                bool keyEsc = false;
                bool keySpace = false;
                bool keyUp = false, keyUpOld = false;
                bool keyDown = false, keyDownOld = false;
                bool keyLeft = false, keyLeftOld = false;
                bool keyRight = false, keyRightOld = false;

                bool enableMirroring = true;
                int snakeDirection = DIRECTION_LEFT;
                Queue<int> input = new Queue<int>();

                LinkedList<SnakeSegment> snake = new LinkedList<SnakeSegment>();
                for (int i = 0; i < 20; i++)
                    snake.AddLast(new SnakeSegment { X = 50 + i, Y = 15 });

                while (!dead)
                {
                    double ms = MAX_FRAME_DELAY - speed;
                    ms = Math.Clamp(ms, MIN_FRAME_DELAY, MAX_FRAME_DELAY);
                    // Compensate for aspect ratio of command line
                    if (snakeDirection % 2 == 0)
                        ms *= 1.2;

                    // Frame timing & input
                    var delay = TimeSpan.FromMilliseconds(ms);
                    var t1 = DateTime.Now;
                    while (DateTime.Now - t1 < delay)
                    {
                        keyUp = (0x8000 & GetAsyncKeyState(VKey.UpArrow)) != 0;
                        keyDown = (0x8000 & GetAsyncKeyState(VKey.DownArrow)) != 0;
                        keyLeft = (0x8000 & GetAsyncKeyState(VKey.LeftArrow)) != 0;
                        keyRight = (0x8000 & GetAsyncKeyState(VKey.RightArrow)) != 0;
                        keyEsc = (0x8000 & GetAsyncKeyState(VKey.Esc)) != 0;

                        if (keyEsc)
                            Environment.Exit(0);

                        if (keyUp && !keyUpOld)
                            if (input.Count < MAX_INPUT_HISTORY)
                                input.Enqueue(DIRECTION_UP);

                        if (keyRight && !keyRightOld)
                            if (input.Count < MAX_INPUT_HISTORY)
                                input.Enqueue(DIRECTION_RIGHT);

                        if (keyDown && !keyDownOld)
                            if (input.Count < MAX_INPUT_HISTORY)
                                input.Enqueue(DIRECTION_DOWN);

                        if (keyLeft && !keyLeftOld)
                            if (input.Count < MAX_INPUT_HISTORY)
                                input.Enqueue(DIRECTION_LEFT);

                        keyUpOld = keyUp;
                        keyDownOld = keyDown;
                        keyLeftOld = keyLeft;
                        keyRightOld = keyRight;
                    }

                    // =============
                    //     Logic
                    // =============

                    // Get next valid direction from the input queue
                    while (input.Count > 0)
                    {
                        int nextSnakeDirection = input.Dequeue();
                        if (nextSnakeDirection % 2 != snakeDirection % 2)
                        {
                            snakeDirection = nextSnakeDirection;
                            break;
                        }
                    }

                    // Update snake position: place a new head at
                    // the front of the list in the right direction
                    switch (snakeDirection)
                    {
                        case DIRECTION_UP:
                            snake.AddFirst(new SnakeSegment
                            {
                                X = snake.First.Value.X,
                                Y = snake.First.Value.Y - 1
                            });
                            break;
                        case DIRECTION_RIGHT:
                            snake.AddFirst(new SnakeSegment
                            {
                                X = snake.First.Value.X + 1,
                                Y = snake.First.Value.Y
                            });
                            break;
                        case DIRECTION_DOWN:
                            snake.AddFirst(new SnakeSegment
                            {
                                X = snake.First.Value.X,
                                Y = snake.First.Value.Y + 1
                            });
                            break;
                        case DIRECTION_LEFT:
                            snake.AddFirst(new SnakeSegment
                            {
                                X = snake.First.Value.X - 1,
                                Y = snake.First.Value.Y
                            });
                            break;
                    }

                    // Collision detect snake vs food
                    if (snake.First.Value.X == foodX && snake.First.Value.Y == foodY)
                    {
                        score += 10;
                        speed += 2;

                        while (screen[foodY * SCREEN_WIDTH + foodX] != ' ')
                        {
                            foodX = rnd.Next(SCREEN_WIDTH);
                            foodY = rnd.Next(SCREEN_HEIGHT);
                            foodX = Math.Clamp(foodX, 0, SCREEN_WIDTH - 1);
                            foodY = Math.Clamp(foodY, 3, SCREEN_HEIGHT - 1);
                        }

                        for (int i = 0; i < 3; i++)
                            snake.AddLast(new SnakeSegment { X = snake.Last.Value.X, Y = snake.Last.Value.Y });
                    }

                    if (enableMirroring)
                    {
                        bool dirty = false;
                        var head = snake.First.Value;

                        if (head.X < 0) { head.X = SCREEN_WIDTH - 1; dirty = true; }
                        if (head.Y < 3) { head.Y = SCREEN_HEIGHT - 1; dirty = true; }
                        if (head.X >= SCREEN_WIDTH) { head.X = 0; dirty = true; }
                        if (head.Y >= SCREEN_HEIGHT) { head.Y = 3; dirty = true; }

                        if (dirty)
                        {
                            snake.RemoveFirst();
                            snake.AddFirst(head);
                        }
                    }
                    else
                    {
                        // Collision detect snake vs world
                        if (snake.First.Value.X < 0 || snake.First.Value.X >= SCREEN_WIDTH)
                            dead = true;
                        if (snake.First.Value.Y < 3 || snake.First.Value.Y >= SCREEN_HEIGHT)
                            dead = true;
                    }

                    // Collision detect snake vs snake
                    var node = snake.First;
                    while (node.Next != null)
                    {
                        node = node.Next;
                        if (node.Value.X == snake.First.Value.X && node.Value.Y == snake.First.Value.Y)
                        {
                            dead = true;
                            break;
                        }
                    }

                    // Chop off snakes tail :-/
                    if (!dead)
                        snake.RemoveLast();

                    // =============
                    // Visualization
                    // =============

                    ClearScreen();

                    // Draw stats & borders
                    for (int x = 0; x < SCREEN_WIDTH; x++)
                    {
                        WriteScreenChar(x, 0, '-');
                        WriteScreenChar(x, 2, '-');
                    }
                    WriteScreenText(1, 1, $"SCORE: {score}");
                    WriteScreenText(SCREEN_WIDTH / 2 - 3, 1, "SNAKE!");

                    // Draw snake body
                    foreach (var segment in snake)
                        WriteScreenChar(segment.X, segment.Y, dead ? '+' : 'O');

                    // Draw snake head
                    WriteScreenChar(snake.First.Value.X, snake.First.Value.Y, dead ? 'X' : '@');

                    // Draw food
                    WriteScreenChar(foodX, foodY, '%');

                    if (dead)
                        WriteScreenText(SCREEN_WIDTH / 2 - 20, 1, "Press 'SPACE' to play again, 'ESC' to exit.");

                    DisplayFrame();
                }

                while (true)
                {
                    keyEsc = (0x8000 & GetAsyncKeyState(VKey.Esc)) != 0;
                    keySpace = (0x8000 & GetAsyncKeyState(VKey.Spacebar)) != 0;

                    if (keyEsc)
                        Environment.Exit(0);

                    if (keySpace) // Play again
                        break;
                }
            }
        }

        public static void ClearScreen()
        {
            for (int i = 0; i < SCREEN_WIDTH * SCREEN_HEIGHT; i++)
                screen[i] = ' ';
        }

        public static void WriteScreenChar(int x, int y, char c)
        {
            if (x < 0 || y < 0 || x >= SCREEN_WIDTH || y >= SCREEN_HEIGHT)
                return;

            screen[y * SCREEN_WIDTH + x] = c;
        }

        public static void WriteScreenText(int x, int y, string s)
        {
            if (x < 0 || y < 0 || x >= SCREEN_WIDTH || y >= SCREEN_HEIGHT)
                return;

            if (string.IsNullOrEmpty(s))
                return;

            int startIndex = y * SCREEN_WIDTH + x;
            int maxIndex = SCREEN_WIDTH * SCREEN_HEIGHT;

            for (int i = 0; i < s.Length; i++)
            {
                int index = startIndex + i;
                if (index < maxIndex)
                    screen[index] = s[i];
                else
                    break;
            }
        }

        public static void DisplayFrame()
        {
            if (hConsole != INVALID_HANDLE_VALUE)
            {
                var str = new string(screen);
                WriteConsoleOutputCharacter(hConsole, str, (uint)str.Length, new COORD { X = 0, Y = 0 }, out uint charsWritten);
            }
        }

        // ===================================================================
        //                             WinAPI
        // ===================================================================

        [DllImport("User32.dll")]
        static extern short GetAsyncKeyState(VKey vKey);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool WriteConsoleOutputCharacter(
            IntPtr hConsoleOutput,
            string lpCharacter,
            uint nLength,
            COORD dwWriteCoord,
            out uint lpNumberOfCharsWritten);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetCurrentConsoleFontEx(
               IntPtr consoleOutput,
               bool maximumWindow,
               ref CONSOLE_FONT_INFO_EX lpConsoleCurrentFontEx);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetCurrentConsoleFontEx(
               IntPtr consoleOutput,
               bool maximumWindow,
               ref CONSOLE_FONT_INFO_EX consoleCurrentFontEx);

        private const int STD_OUTPUT_HANDLE = -11;
        private const int TMPF_TRUETYPE = 4;
        private const int LF_FACESIZE = 32;
        private static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COORD
        {
            internal short X;
            internal short Y;

            internal COORD(short x, short y)
            {
                X = x;
                Y = y;
            }
        }

        // Notes:
        // https://www.pinvoke.net/default.aspx/kernel32.SetCurrentConsoleFontEx
        // https://stackoverflow.com/questions/20631634/changing-font-in-a-console-window-in-c-sharp
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CONSOLE_FONT_INFO_EX
        {
            internal uint cbSize;
            internal uint nFont;
            internal COORD dwFontSize;
            internal int FontFamily;
            internal int FontWeight;

            // Instead of "fixed char FaceName[LF_FACESIZE]"
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            internal string FaceName;
        }

        // https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        enum VKey : Int32
        {
            Enter = 0x0D,
            Esc = 0x1B,
            Spacebar = 0x20,
            LeftArrow = 0x25,
            UpArrow = 0x26,
            RightArrow = 0x27,
            DownArrow = 0x28,
        }
    }
}
