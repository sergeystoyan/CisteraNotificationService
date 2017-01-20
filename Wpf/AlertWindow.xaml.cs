﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.Windows.Interop;
using System.Threading;
using System.Windows.Media.Animation;

namespace Cliver.CisteraNotification
{
    public partial class AlertWindow : Window
    {
        static AlertWindow()
        {
        }

        static Window invisible_owner_w;

        static System.Windows.Threading.Dispatcher dispatcher = null;
        static AlertWindow This = null;
        static object lock_object = new object();

        public static AlertWindow Create(string title, string text, string image_url, string action_name, Action action)
        {
            lock (lock_object)
            {
                Action a = () =>
                {
                    if (This != null)
                    {
                        try
                        {//might be closed already
                            This.Close();
                        }
                        catch
                        { }
                    }

                    This = new AlertWindow(title, text, image_url, action_name, action);
                    WindowInteropHelper h = new WindowInteropHelper(This);
                    h.EnsureHandle();
                    This.Show();
                    //ThreadRoutines.StartTry(() =>
                    //{
                    //    Thread.Sleep(Settings.Default.InfoWindowLifeTimeInSecs * 1000);
                    //    This.BeginInvoke(() => { This.Close(); });
                    //});
                    if (!string.IsNullOrWhiteSpace(Settings.Default.InfoSoundFile))
                    {
                        SoundPlayer sp = new SoundPlayer(Settings.Default.AlertSoundFile);
                        sp.Play();
                    }
                };

                if (dispatcher == null)
                {//!!!the following code does not work in static constructor because creates a deadlock!!!
                    ThreadRoutines.StartTry(() =>
                    {
                        //this window is used to hide notification windows from Alt+Tab panel
                        invisible_owner_w = new Window();
                        invisible_owner_w.Width = 0;
                        invisible_owner_w.Height = 0;
                        invisible_owner_w.WindowStyle = WindowStyle.ToolWindow;
                        invisible_owner_w.ShowInTaskbar = false;
                        invisible_owner_w.Show();
                        invisible_owner_w.Hide();

                        dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                        System.Windows.Threading.Dispatcher.Run();
                    }, null, null, true, ApartmentState.STA);
                    if (!SleepRoutines.WaitForCondition(() => { return dispatcher != null; }, 3000))
                        throw new Exception("Could not get dispatcher.");
                }
                dispatcher.Invoke(a);
                return This;
            }
        }

        AlertWindow()
        {
            InitializeComponent();
        }

        AlertWindow(string title, string text, string image_url, string action_name, Action action)
        {
            InitializeComponent();

            Loaded += Window_Loaded;
            Closing += Window_Closing;
            PreviewMouseDown += (object sender, MouseButtonEventArgs e) =>
            {
                try
                {//might be closed already
                    Close();
                }
                catch { }
            };

            Topmost = true;
            Owner = invisible_owner_w;

            this.title.Text = title;
            this.text.Text = text;
            //var request = System.Net.WebRequest.Create(image_url);
            //request.BeginGetResponse((r) =>
            //{
            //    if (!r.IsCompleted)
            //        return;
            //    using (var stream = ((System.Net.WebRequest)r.AsyncState).EndGetResponse(r).GetResponseStream())
            //    {
            //        image.Image = Bitmap.FromStream(stream);
            //    }
            //}, request);
            if (image_url != null)
            {
                if (!image_url.Contains(":"))
                    image_url = Log.AppDir + image_url;
                try
                {
                    image.Source = new BitmapImage(new Uri(image_url));
                }
                catch
                {
                }
            }
            else
            {
                image.Width = 0;
                image.Height = 0;
            }
            if (action_name != null)
                this.button.Content = action_name;
            this.button.Click += (object sender, RoutedEventArgs e) =>
            {
                action?.Invoke();
                Close();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var a = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(UIElement.OpacityProperty, a);

            Rect wa = System.Windows.SystemParameters.WorkArea;

            Storyboard sb = new Storyboard();
            DoubleAnimation da = new DoubleAnimation(-Height, Height, (Duration)TimeSpan.FromMilliseconds(300));
            Storyboard.SetTargetProperty(da, new PropertyPath("(Top)")); //Do not miss the '(' and ')'
            sb.Children.Add(da);
            BeginStoryboard(sb);
        }

        //new double Top
        //{
        //    get
        //    {
        //        return (double)this.Invoke(() => { return base.Top; });
        //    }
        //    set
        //    {
        //        base.Top = value;
        //    }
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            var a = new System.Windows.Media.Animation.DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(300));
            a.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, a);
        }
    }
}