﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace eye_tracking_mouse
{
    public class InputManager
    {
        private readonly Interceptor.Input driver_input = new Interceptor.Input();
        private readonly EyeTrackingMouse eye_tracking_mouse;

        // For hardcoded stop-word.
        private bool is_win_pressed = false;
        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        private Action<ReadKeyResult> read_key_callback;

        public enum KeyState
        {
            Up,
            Down,
        };

        private struct InteractionHistoryEntry
        {
            public Key Key;
            public KeyState State;
            public DateTime Time;
        };

        public void Stop()
        {
            lock (Helpers.locker)
            {
                eye_tracking_mouse.StopControlling();
                if (driver_input.IsLoaded)
                {
                    driver_input.Unload();
                    driver_input.OnKeyPressed -= OnKeyPressedInterceptionDriver;
                }

                if (win_api_hook_id != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(win_api_hook_id);
                    win_api_hook_id = IntPtr.Zero;
                }
            }
        }

        private void SendModifierDown()
        {
            lock (Helpers.locker)
            {
                if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver && driver_input.IsLoaded)
                {
                    driver_input.SendKey(Options.Instance.key_bindings[Key.Modifier], Options.Instance.key_bindings.is_modifier_e0 ? Interceptor.KeyState.E0 : Interceptor.KeyState.Down);
                    Thread.Sleep(Options.Instance.win_press_delay_ms);
                }
                else
                {
                    ignore_next_key_press = true;
                    keybd_event((byte)System.Windows.Forms.Keys.LWin, 0, 1, 0);
                }
            }
        }

        private bool OnKeyPressed(Key key, KeyState key_state, bool is_modifier)
        {
            lock (Helpers.locker)
            {
                // If you hold a key pressed for a second it will start to produce a sequence of rrrrrrrrrrepeated |KeyState.Down| events.
                // For some keys we don't want to handle such events and assume that a key stays pressed until |KeyState.Up| appears.
                bool is_repetition = interaction_history[0].Key == key &&
                    interaction_history[0].State == key_state &&
                    key_state == KeyState.Down;

                if (!is_repetition)
                {
                    interaction_history[2] = interaction_history[1];
                    interaction_history[1] = interaction_history[0];
                    interaction_history[0].Key = key;
                    interaction_history[0].State = key_state;
                    interaction_history[0].Time = DateTime.Now;
                }

                bool is_double_press =
                    key_state == KeyState.Down &&
                    interaction_history[1].Key == key &&
                    interaction_history[2].Key == key &&
                    (DateTime.Now - interaction_history[2].Time).TotalMilliseconds < Options.Instance.double_click_duration_ms;

                bool is_short_press =
                    key_state == KeyState.Up &&
                    interaction_history[1].Key == key &&
                    (DateTime.Now - interaction_history[1].Time).TotalMilliseconds < Options.Instance.short_click_duration_ms;

                return eye_tracking_mouse.OnKeyPressed(key, key_state, is_double_press, is_short_press, is_repetition, is_modifier, SendModifierDown);
            }
        }

        public struct ReadKeyResult
        {
            public Interceptor.Keys key;
            public bool is_e0_key;
        }
        public void ReadKeyAsync(Action<ReadKeyResult> callback)
        {
            lock (Helpers.locker)
            {
                read_key_callback = callback;
            }
        }

        private IntPtr win_api_hook_id = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc win_api_callback;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private bool ignore_next_key_press = false;
        private IntPtr OnKeyPressedWinApi(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (ignore_next_key_press)
            {
                ignore_next_key_press = false;
                return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
            }
            if (nCode >= 0)
            {
                Dictionary<System.Windows.Forms.Keys, Key> bindings = new Dictionary<System.Windows.Forms.Keys, Key>
                {
                    {System.Windows.Forms.Keys.LWin, Key.Modifier},
                    {System.Windows.Forms.Keys.J, Key.LeftMouseButton},
                    {System.Windows.Forms.Keys.K, Key.RightMouseButton},
                    {System.Windows.Forms.Keys.N, Key.ScrollDown},
                    {System.Windows.Forms.Keys.H,Key.ScrollUp},
                    {System.Windows.Forms.Keys.Oemcomma,Key.ScrollLeft},
                    {System.Windows.Forms.Keys.OemPeriod, Key.ScrollRight},
                    {System.Windows.Forms.Keys.M, Key.ShowCalibrationView},
                    {System.Windows.Forms.Keys.A, Key.CalibrateLeft},
                    {System.Windows.Forms.Keys.D,Key.CalibrateRight},
                    {System.Windows.Forms.Keys.W, Key.CalibrateUp},
                    {System.Windows.Forms.Keys.S, Key.CalibrateDown},
                };

                System.Windows.Forms.Keys key_code = (System.Windows.Forms.Keys)Marshal.ReadInt32(lParam);
                Key key = Key.Unbound;
                if (bindings.ContainsKey(key_code))
                    key = bindings[key_code];
                KeyState key_state;

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    key_state = KeyState.Down;
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    key_state = KeyState.Up;
                }
                else
                {
                    return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
                }

                if (OnKeyPressed(key, key_state, Helpers.IsModifier(key_code)))
                    return new IntPtr(1);
            }

            return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
        }

        public void OnKeyPressedInterceptionDriver(object sender, Interceptor.KeyPressedEventArgs e)
        {
            // Console.WriteLine(e.Key);
            // Console.WriteLine(e.State);

            lock (Helpers.locker)
            {
                e.Handled = true;

                // Interceptor.KeyState is a mess. Different Keys produce different KeyState when pressed and released.
                bool is_e0_key = (e.State & Interceptor.KeyState.E0) != 0;
                KeyState key_state;
                if (e.State == Interceptor.KeyState.E0 || e.State == Interceptor.KeyState.Down)
                {
                    key_state = KeyState.Down;
                }
                else if ((e.State & Interceptor.KeyState.Up) != 0)
                {
                    key_state = KeyState.Up;
                }
                else
                {
                    e.Handled = false;
                    return;
                }

                // Hardcoded stop-word is Win+Del.
                if (e.Key == Interceptor.Keys.WindowsKey)
                {
                    if (key_state == KeyState.Down)
                        is_win_pressed = true;
                    else if (key_state == KeyState.Up)
                        is_win_pressed = false;
                }
                if (e.Key == Interceptor.Keys.Delete &&
                    key_state == KeyState.Down && is_win_pressed)
                {
                    Environment.Exit(0);
                    return;
                }

                if (key_state == KeyState.Down && read_key_callback != null)
                {
                    read_key_callback(new ReadKeyResult { is_e0_key = is_e0_key, key = e.Key });
                    read_key_callback = null;
                    e.Handled = true;
                    return;
                }

                // Convert |Interceptor.Keys| to |eye_tracking_mouse.Key|
                var key_bindings = Options.Instance.key_bindings;
                Key key = Key.Unbound;
                if (key_bindings.bindings.ContainsValue(e.Key))
                {
                    key = key_bindings.bindings.First(pair =>
                    {
                        return pair.Value == e.Key;
                    }).Key;
                }

                if (key == Key.Modifier && key_bindings.is_modifier_e0 != is_e0_key)
                    key = Key.Unbound;

                e.Handled = OnKeyPressed(key, key_state, Helpers.IsModifier(e.Key));
            }
        }

        // Enables keys interception with selected |interception_method|. Backs off to WinAPI if failed loading interception driver.
        public bool UpdateInterceptionMethod()
        {
            lock (Helpers.locker)
            {
                Stop();

                if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver)
                {
                    driver_input.OnKeyPressed += OnKeyPressedInterceptionDriver;
                    driver_input.KeyboardFilterMode = Interceptor.KeyboardFilterMode.All;

                    if (driver_input.Load())
                        return true;
                }

                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    win_api_hook_id = SetWindowsHookEx(WH_KEYBOARD_LL, win_api_callback,
                        GetModuleHandle(curModule.ModuleName), 0);
                }

                return Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.WinApi;
            }
        }

        public bool IsDriverLoaded()
        {
            lock (Helpers.locker)
            {
                return driver_input.IsLoaded;
            }
        }

        public InputManager(EyeTrackingMouse eye_tracking_mouse)
        {
            this.eye_tracking_mouse = eye_tracking_mouse;
            win_api_callback = OnKeyPressedWinApi;
            if (!UpdateInterceptionMethod())
            {
                MessageBox.Show(
                    "Failed loading interception driver." +
                    "Reinstall EyeTrackingMouse or install the driver from command line: " +
                    "https://github.com/oblitum/Interception. Application will run using WinAPI.",
                    Helpers.application_name, MessageBoxButton.OK, MessageBoxImage.Warning);
                Options.Instance.key_bindings.interception_method = KeyBindings.InterceptionMethod.WinApi;
            }
        }
    }
}
