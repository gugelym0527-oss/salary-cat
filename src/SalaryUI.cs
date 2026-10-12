using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SalaryPet {
    public enum CatMood { Normal, Smile, Confused, Sulky, Angry }
    // Ephemeral animation state, deliberately separate from payroll and saved configuration.
    public sealed class CatReaction {
        public CatMood Mood {get;private set;}
        int clicks;long lastClick,expires;
        public void Click(long milliseconds,int gap){
            Refresh(milliseconds);
            if(Mood==CatMood.Angry)return;
            if(clicks==0||milliseconds-lastClick>gap)clicks=0;
            clicks=Math.Min(4,clicks+1);lastClick=milliseconds;expires=milliseconds+2000;
            Mood=(CatMood)clicks;
        }
        public bool Refresh(long milliseconds){
            if(Mood==CatMood.Normal||milliseconds<expires)return false;
            Mood=CatMood.Normal;clicks=0;return true;
        }
    }
    public static class Art {
        public static Color Ink=Color.FromArgb(43,65,60),Muted=Color.FromArgb(105,120,118),Green=Color.FromArgb(74,122,103);
        public static GraphicsPath Round(Rectangle r,int radius){GraphicsPath p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static void Text(Graphics g,string s,float size,Color color,float x,float y,string family="Microsoft YaHei UI"){using(Font f=new Font(family,size,FontStyle.Regular,GraphicsUnit.Pixel))using(Brush b=new SolidBrush(color))g.DrawString(s,f,b,x,y);}
        public static void Cat(Graphics g,float x,float y,float s,int second,CatMood mood=CatMood.Normal){
            string[] sprite={"0110000000110","1111000011110","1111111111110","1111111111110","1111111111110","1111111111110","0111111111100","0011111111000"};
            using(Brush b=new SolidBrush(mood==CatMood.Angry?Color.FromArgb(192,87,88):Green))for(int row=0;row<sprite.Length;row++)for(int col=0;col<sprite[row].Length;col++)if(sprite[row][col]=='1')g.FillRectangle(b,x+col*s,y+row*s,s,s);
            using(Pen p=new Pen(Color.White,.65f*s))using(Brush b=new SolidBrush(Color.White)){
                if(mood==CatMood.Normal){float h=second%6==0?.55f*s:1.6f*s;g.FillRectangle(b,x+3*s,y+3.6f*s,s,h);g.FillRectangle(b,x+9*s,y+3.6f*s,s,h);g.FillRectangle(b,x+6*s,y+5.6f*s,s,.6f*s);}
                else if(mood==CatMood.Smile){
                    g.DrawLines(p,new[]{new PointF(x+2.5f*s,y+4.5f*s),new PointF(x+3.5f*s,y+3.7f*s),new PointF(x+4.5f*s,y+4.5f*s)});
                    g.DrawLines(p,new[]{new PointF(x+8.5f*s,y+4.5f*s),new PointF(x+9.5f*s,y+3.7f*s),new PointF(x+10.5f*s,y+4.5f*s)});
                    g.DrawArc(p,x+5*s,y+4.8f*s,3*s,1.7f*s,0,180);
                }else if(mood==CatMood.Confused){
                    g.FillRectangle(b,x+3*s,y+3.4f*s,s,1.8f*s);g.DrawLine(p,x+8.5f*s,y+4.1f*s,x+10.5f*s,y+4.1f*s);
                    g.DrawEllipse(p,x+6*s,y+5.7f*s,s,s);Text(g,"?",3.6f*s,Green,x+12*s,y-1.8f*s,"Consolas");
                }else if(mood==CatMood.Sulky){
                    g.DrawLine(p,x+2.5f*s,y+3.9f*s,x+4.5f*s,y+4.4f*s);g.DrawLine(p,x+8.5f*s,y+4.4f*s,x+10.5f*s,y+3.9f*s);
                    g.DrawArc(p,x+5.3f*s,y+5.8f*s,2.5f*s,1.2f*s,180,180);
                }else{
                    g.DrawLine(p,x+2.5f*s,y+3.3f*s,x+4.5f*s,y+4.2f*s);g.DrawLine(p,x+8.5f*s,y+4.2f*s,x+10.5f*s,y+3.3f*s);
                    g.FillRectangle(b,x+3.5f*s,y+4.5f*s,.7f*s,s);g.FillRectangle(b,x+9*s,y+4.5f*s,.7f*s,s);
                    g.DrawLines(p,new[]{new PointF(x+5*s,y+6.6f*s),new PointF(x+6.5f*s,y+5.8f*s),new PointF(x+8*s,y+6.6f*s)});
                }
            }
        }
        public static void Digits(Graphics g,string text,RectangleF box,Color color){
            float units=0;foreach(char ch in text)units+=ch=='.'?.22f:ch==':'?.25f:.60f;
            float h=Math.Min(box.Height,box.Width/units),x=box.X+(box.Width-h*units)/2;
            int[] masks={0x3f,0x06,0x5b,0x4f,0x66,0x6d,0x7d,0x07,0x7f,0x6f};
            foreach(char ch in text){
                if(ch=='.'){using(Brush b=new SolidBrush(color))g.FillRectangle(b,x+h*.04f,box.Y+h*.88f,h*.095f,h*.095f);x+=h*.22f;continue;}
                if(ch==':'){using(Brush b=new SolidBrush(color)){g.FillRectangle(b,x+h*.065f,box.Y+h*.29f,h*.1f,h*.1f);g.FillRectangle(b,x+h*.065f,box.Y+h*.66f,h*.1f,h*.1f);}x+=h*.25f;continue;}
                int n=ch-'0';if(n>=0&&n<=9){float w=h*.51f,t=h*.085f;
                    RectangleF[] a={new RectangleF(x+t,box.Y,w-2*t,t),new RectangleF(x+w-t,box.Y+t,t,h/2-1.5f*t),new RectangleF(x+w-t,box.Y+h/2+t/2,t,h/2-1.5f*t),new RectangleF(x+t,box.Y+h-t,w-2*t,t),new RectangleF(x,box.Y+h/2+t/2,t,h/2-1.5f*t),new RectangleF(x,box.Y+t,t,h/2-1.5f*t),new RectangleF(x+t,box.Y+h/2-t/2,w-2*t,t)};
                    for(int i=0;i<7;i++)using(Brush b=new SolidBrush((masks[n]&(1<<i))!=0?color:Color.FromArgb(8,color)))g.FillRectangle(b,a[i]);
                }x+=h*.6f;
            }
        }
        public static Bitmap IconBitmap(int size){Bitmap b=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(Graphics g=Graphics.FromImage(b)){g.Clear(Color.Transparent);float scale=size/16f;Cat(g,scale*1.5f,scale*3.5f,scale,1);}return b;}
        public static void WriteIcon(string path){
            int[] sizes={16,32,48,64,128,256};List<byte[]> data=new List<byte[]>();
            foreach(int s in sizes)using(Bitmap b=IconBitmap(s))using(MemoryStream stream=new MemoryStream()){b.Save(stream,ImageFormat.Png);data.Add(stream.ToArray());}
            using(BinaryWriter w=new BinaryWriter(File.Create(path))){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
                for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(data[i].Length);w.Write(offset);offset+=data[i].Length;}
                foreach(byte[] item in data)w.Write(item);
            }
        }
        public static Icon LoadIcon(){string path=Path.Combine(Store.Folder,"电子猫.ico");return File.Exists(path)?new Icon(path):SystemIcons.Application;}
        static IEnumerable<Control> Descendants(Control root){foreach(Control child in root.Controls){yield return child;foreach(Control nested in Descendants(child))yield return nested;}}
        public static void Snapshot(Form form,string path){
            using(Bitmap b=new Bitmap(form.Width,form.Height)){
                form.DrawToBitmap(b,new Rectangle(0,0,b.Width,b.Height));
                // Native dropdowns can omit their selection text from WM_PRINT.
                // Render the actual selected item into the application's static preview.
                using(Graphics g=Graphics.FromImage(b))foreach(ComboBox choice in Descendants(form).OfType<ComboBox>().Where(c=>c.Visible&&c.SelectedIndex>=0)){
                    Point screen=choice.PointToScreen(Point.Empty);Rectangle area=new Rectangle(screen.X-form.Left+1,screen.Y-form.Top+1,choice.Width-20,choice.Height-2);
                    using(Brush white=new SolidBrush(Color.White))g.FillRectangle(white,area);
                    area.X+=4;area.Width-=4;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using(Brush ink=new SolidBrush(Art.Ink))using(StringFormat format=new StringFormat{LineAlignment=StringAlignment.Center})g.DrawString(Convert.ToString(choice.SelectedItem),choice.Font,ink,area,format);
                }
                b.Save(path,ImageFormat.Png);
            }
        }
    }
    public class PetButton:Button {
        public PetButton(string text){Text=text;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.FromArgb(235,241,237);ForeColor=Art.Ink;Font=new Font("Microsoft YaHei UI",8f);Cursor=Cursors.Hand;UseVisualStyleBackColor=false;}
    }
    public class WhiteForm:Form {
        public WhiteForm(){Font=new Font("Microsoft YaHei UI",9f);ForeColor=Art.Ink;BackColor=Color.White;Icon=Art.LoadIcon();StartPosition=FormStartPosition.CenterParent;AutoScaleMode=AutoScaleMode.None;}
        protected Label LabelAt(Control parent,string text,int x,int y,int w,int h=25,float size=9f){Label l=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Microsoft YaHei UI",size),ForeColor=Art.Ink};parent.Controls.Add(l);return l;}
        protected PetButton ButtonAt(Control parent,string text,int x,int y,int w=100,int h=30){PetButton b=new PetButton(text){Location=new Point(x,y),Size=new Size(w,h)};parent.Controls.Add(b);return b;}
    }
    public class MoneyBarForm:Form {
        readonly ClockForm clock;string amount="¥0.00";
        [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindow(string className,string title);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string className,string title);
        [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr window,out NativeRect rect);
        public MoneyBarForm(ClockForm owner){
            clock=owner;AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;
            BackColor=Color.White;ClientSize=new Size(108,28);StartPosition=FormStartPosition.Manual;AccessibleName="今日收入金额条，双击恢复桌宠";
            MouseDoubleClick+=delegate{clock.Restore();};
            using(GraphicsPath shape=Art.Round(new Rectangle(0,0,Width,Height),8))Region=new Region(shape);
        }
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override CreateParams CreateParams{get{CreateParams cp=base.CreateParams;cp.ExStyle|=0x80|0x08000000;return cp;}}
        public void RefreshMoney(string money){
            amount=money;Text=money;AccessibleDescription="今日已赚 "+money+"，双击恢复桌宠";
            int desiredWidth=Math.Max(108,money.Length*10+14);
            if(Width!=desiredWidth){Width=desiredWidth;Region previous=Region;using(GraphicsPath shape=Art.Round(new Rectangle(0,0,Width,Height),8))Region=new Region(shape);if(previous!=null)previous.Dispose();}
            DockBesideTray();Invalidate();
        }
        void DockBesideTray(){
            Rectangle screen=Screen.PrimaryScreen.Bounds;NativeRect taskbarRect,notifyRect;
            IntPtr taskbar=FindWindow("Shell_TrayWnd",null),notify=FindWindowEx(taskbar,IntPtr.Zero,"TrayNotifyWnd",null);
            Point next;
            if(taskbar!=IntPtr.Zero&&GetWindowRect(taskbar,out taskbarRect)&&taskbarRect.Bottom-taskbarRect.Top>=Height&&taskbarRect.Right-taskbarRect.Left>taskbarRect.Bottom-taskbarRect.Top){
                int right=notify!=IntPtr.Zero&&GetWindowRect(notify,out notifyRect)?notifyRect.Left:taskbarRect.Right-200;
                next=new Point(Math.Max(taskbarRect.Left+8,right-Width-6),Math.Max(screen.Top,Math.Min(screen.Bottom-Height,taskbarRect.Top+(taskbarRect.Bottom-taskbarRect.Top-Height)/2)));
            }else next=new Point(screen.Right-Width-220,screen.Bottom-Height-6);
            if(Location!=next)Location=next;
        }
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Color.White);
            using(GraphicsPath p=Art.Round(new Rectangle(0,0,Width-1,Height-1),8))using(Pen pen=new Pen(Color.FromArgb(219,229,222)))e.Graphics.DrawPath(pen,p);
            using(Font font=new Font("Consolas",16f,FontStyle.Regular,GraphicsUnit.Pixel))using(Brush ink=new SolidBrush(Art.Ink))using(StringFormat format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})e.Graphics.DrawString(amount,font,ink,new RectangleF(3,1,Width-6,Height-2),format);
        }
    }
    public class ClockForm:Form {
        public Config Config;public Result Current;public readonly bool Preview;
        public MoneyBarForm MoneyBar{get;private set;}
        Timer timer,reactionTimer;NotifyIcon tray;ContextMenuStrip menu;Image wallpaper;PetButton overtime,earlyLeave;
        readonly CatReaction reaction=new CatReaction();
        readonly System.Diagnostics.Stopwatch reactionClock=System.Diagnostics.Stopwatch.StartNew();
        public static readonly Rectangle CatBounds=new Rectangle(172,28,47,30);
        bool catPressed;
        public CatMood Mood{get{return reaction.Mood;}}
        public decimal DisplayedToday{get{return Mood==CatMood.Angry||Current==null?0m:Current.Today.Total;}}
        bool allowExit,promptOpen,positionDirty,initializing=true;DateTime positionChanged;
        public ClockForm(Config c,bool preview=false){
            Config=c;Preview=preview;Text="工资桌宠 · 东八区";Icon=Art.LoadIcon();ClientSize=new Size(230,193);
            AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;DoubleBuffered=true;BackColor=Color.White;TopMost=c.TopMost;
            StartPosition=FormStartPosition.Manual;Rectangle a=Screen.PrimaryScreen.WorkingArea;Location=new Point(a.Right-Width-24,a.Bottom-Height-24);
            if(c.Left>=0&&Screen.AllScreens.Any(s=>s.WorkingArea.IntersectsWith(new Rectangle(c.Left,c.Top,Width,Height))))Location=new Point(c.Left,c.Top);
            using(GraphicsPath p=Art.Round(new Rectangle(0,0,Width,Height),18))Region=new Region(p);
            PetButton min=new PetButton("−"){Bounds=new Rectangle(177,7,20,19)},close=new PetButton("×"){Bounds=new Rectangle(201,7,20,19)};
            min.AccessibleName="最小化到任务栏，标题显示今日金额";min.Click+=delegate{ShowInTaskbar=true;WindowState=FormWindowState.Minimized;};
            close.AccessibleName=c.CloseToTray?"收起到系统托盘":"退出工资桌宠";close.Click+=delegate{Close();};
            PetButton settings=new PetButton("设置"){Bounds=new Rectangle(12,164,55,23)};settings.Click+=delegate{OpenSettings();};
            overtime=new PetButton("加班"){Bounds=new Rectangle(73,164,55,23)};overtime.Click+=delegate{ToggleOvertime();};
            earlyLeave=new PetButton("提前下班"){Bounds=new Rectangle(134,164,84,23),AccessibleName="提前下班，立即结算全天底薪和已完成加班"};earlyLeave.Click+=delegate{ToggleSettlement();};
            foreach(PetButton button in new[]{min,close,settings,overtime,earlyLeave}){button.Font=new Font("Microsoft YaHei UI",10f,FontStyle.Regular,GraphicsUnit.Pixel);button.Padding=Padding.Empty;button.TextAlign=ContentAlignment.MiddleCenter;}
            Controls.AddRange(new Control[]{min,close,settings,overtime,earlyLeave});
            menu=new ContextMenuStrip();menu.Items.Add("显示桌宠",null,delegate{Restore();});menu.Items.Add("设置",null,delegate{Restore();OpenSettings();});
            menu.Items.Add("工资明细 / 导出",null,delegate{using(DetailForm f=new DetailForm(Config))f.ShowDialog();});menu.Items.Add("开始 / 结束加班",null,delegate{ToggleOvertime();});
            menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出",null,delegate{ExitPet();});
            tray=new NotifyIcon{Icon=Icon,Text="工资桌宠",ContextMenuStrip=menu,Visible=!Preview};tray.DoubleClick+=delegate{Restore();};
            timer=new Timer{Interval=1000};timer.Tick+=delegate{Tick();};
            reactionTimer=new Timer{Interval=40};reactionTimer.Tick+=delegate{
                if(reaction.Refresh(reactionClock.ElapsedMilliseconds)){reactionTimer.Stop();Tick();}
            };
            Resize+=delegate{if(timer!=null){UpdateMoneyTitle();UpdateMoneyBar();}};
            Shown+=delegate{initializing=false;Tick();timer.Start();};
            LocationChanged+=delegate{if(!initializing&&!Preview&&WindowState==FormWindowState.Normal){positionDirty=true;positionChanged=DateTime.UtcNow;}};
            FormClosing+=OnClosing;
            FormClosed+=delegate{timer.Stop();timer.Dispose();reactionTimer.Stop();reactionTimer.Dispose();reactionClock.Stop();tray.Visible=false;tray.Dispose();menu.Dispose();if(MoneyBar!=null){MoneyBar.Close();MoneyBar.Dispose();}if(wallpaper!=null)wallpaper.Dispose();};
            ReloadBackground();Current=Payroll.Calculate(Config,Payroll.ChinaNow());MoneyBar=new MoneyBarForm(this);
        }
        [DllImport("user32.dll")]static extern bool ReleaseCapture();
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
        protected override void OnMouseDown(MouseEventArgs e){
            base.OnMouseDown(e);
            if(e.Button!=MouseButtons.Left)return;
            if(CatBounds.Contains(e.Location)){catPressed=true;Capture=true;return;}
            ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);
        }
        protected override void OnMouseUp(MouseEventArgs e){
            base.OnMouseUp(e);
            if(e.Button!=MouseButtons.Left)return;
            bool click=catPressed&&CatBounds.Contains(e.Location);catPressed=false;Capture=false;
            if(click){reaction.Click(reactionClock.ElapsedMilliseconds,SystemInformation.DoubleClickTime);reactionTimer.Start();Tick();}
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)catPressed=false;}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);Cursor=CatBounds.Contains(e.Location)?Cursors.Hand:Cursors.Default;}
        string ReactionText{get{
            switch(Mood){case CatMood.Smile:return "嘿嘿，摸摸头真开心";case CatMood.Confused:return "喵？怎么又点我啦";case CatMood.Sulky:return "哼，我有一点小情绪";case CatMood.Angry:return "生气！金额暂时归零";default:return Current==null?"":Current.State;}
        }}
        void UpdateMoneyTitle(){
            if(Current==null)return;
            string money="¥"+DisplayedToday.ToString("F"+Config.Decimals,CultureInfo.InvariantCulture);
            Text=WindowState==FormWindowState.Minimized?money:"工资桌宠 · 东八区";
        }
        void UpdateMoneyBar(){
            if(MoneyBar==null||MoneyBar.IsDisposed||Current==null)return;
            if(Config.TaskbarMoneyBar&&ShowInTaskbar&&WindowState==FormWindowState.Minimized){
                MoneyBar.RefreshMoney("¥"+DisplayedToday.ToString("F"+Config.Decimals,CultureInfo.InvariantCulture));
                if(!MoneyBar.Visible)MoneyBar.Show();
            }else MoneyBar.Hide();
        }
        public void Tick(){
            if(reaction.Refresh(reactionClock.ElapsedMilliseconds))reactionTimer.Stop();
            if(!Preview&&Program.RestoreSignal!=null&&Program.RestoreSignal.WaitOne(0)){Restore();return;}
            DateTime now=Payroll.ChinaNow();Current=Payroll.Calculate(Config,now);
            string money="¥"+DisplayedToday.ToString("F"+Config.Decimals,CultureInfo.InvariantCulture);
            UpdateMoneyTitle();
            UpdateMoneyBar();
            tray.Text=money;overtime.Text=Payroll.LiveSession(Config,now)==null?"加班":"结束加班";
            bool settled=Payroll.IsSettled(Config,now);earlyLeave.Text=settled?"撤销结算":"提前下班";
            earlyLeave.Enabled=settled||Payroll.CanSettle(Config,now);overtime.Enabled=!settled;
            earlyLeave.AccessibleName=settled?"撤销今日结算，恢复按实际时间计薪":"提前下班，立即结算全天底薪和已完成加班";
            AccessibleDescription="今日已赚 "+money+"；本月累计 "+Current.Month.ToString("F2")+"；"+ReactionText;
            if(WindowState!=FormWindowState.Minimized)Invalidate();
            if(!Preview&&positionDirty&&(DateTime.UtcNow-positionChanged).TotalSeconds>=2){try{Store.Change(Config,delegate{Config.Left=Location.X;Config.Top=Location.Y;});positionDirty=false;}catch{positionDirty=false;}}
            if(!Preview&&!promptOpen&&OwnedForms.Length==0&&Payroll.ShouldPrompt(Config,now))AskTonight();
        }
        void AskTonight(){
            promptOpen=true;
            try{
                Store.Change(Config,delegate{Config.Decisions[Payroll.ChinaNow().ToString("yyyy-MM-dd")]="pending";});
                using(OvertimeAskForm ask=new OvertimeAskForm(Config)){
                    DialogResult answer=ask.ShowDialog();
                    if(answer==DialogResult.Yes||answer==DialogResult.No)Store.Change(Config,delegate{Payroll.Decide(Config,Payroll.ChinaNow(),answer==DialogResult.Yes);});
                }
            }catch(Exception ex){MessageBox.Show("无法保存加班选择："+ex.Message,"工资桌宠");}
            finally{promptOpen=false;Current=Payroll.Calculate(Config,Payroll.ChinaNow());Invalidate();}
        }
        public void ToggleSettlement(){
            try{
                DateTime now=Payroll.ChinaNow();
                Store.Change(Config,delegate{if(Payroll.IsSettled(Config,now))Payroll.UndoSettlement(Config,now);else Payroll.Settle(Config,now);});
                Tick();
            }catch(Exception ex){MessageBox.Show("结算操作未保存："+ex.Message,"工资桌宠");}
        }
        void ToggleOvertime(){
            try{
                DateTime now=Payroll.ChinaNow();
                if(Payroll.IsSettled(Config,now)){MessageBox.Show("今日已结算，请先点击“撤销结算”再开始加班。","工资桌宠");return;}
                if(Payroll.LiveSession(Config,now)!=null)Store.Change(Config,delegate{Payroll.Stop(Config,now);});
                else{
                    string time=Config.OvertimeStart;
                    string extra=now.TimeOfDay<Payroll.Time(time)?"将在 "+time+" 开始计时。":"将从现在开始计时（早于 "+time+" 不计加班）。";
                    if(MessageBox.Show("要开始本次加班吗？\n"+extra,"加班计时",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                    Store.Change(Config,delegate{
                        Config.Decisions[now.ToString("yyyy-MM-dd")]="yes";
                        DateTime start=now>now.Date+Payroll.Time(time)?now:now.Date+Payroll.Time(time);
                        Config.Sessions.Add(new Session{Date=now.ToString("yyyy-MM-dd"),Start=Payroll.Stamp(start),End=""});
                    });
                }
                Tick();
            }catch(Exception ex){MessageBox.Show("加班操作未保存："+ex.Message,"工资桌宠");}
        }
        void OnClosing(object sender,FormClosingEventArgs e){
            if(Preview)return;
            if(!allowExit&&e.CloseReason==CloseReason.UserClosing&&Config.CloseToTray){e.Cancel=true;Hide();ShowInTaskbar=false;MoneyBar.Hide();return;}
        }
        void ExitPet(){
            if(Payroll.LiveSession(Config,Payroll.ChinaNow())!=null){
                DialogResult answer=MessageBox.Show("退出前结束今天的加班吗？\n选“否”会继续按时间计算，最晚到当天24:00。","退出工资桌宠",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question);
                if(answer==DialogResult.Cancel)return;
                if(answer==DialogResult.Yes)try{Store.Change(Config,delegate{Payroll.Stop(Config,Payroll.ChinaNow());});}catch(Exception ex){MessageBox.Show(ex.Message);return;}
            }
            allowExit=true;Close();
        }
        public void Restore(){ShowInTaskbar=true;Show();WindowState=FormWindowState.Normal;BringToFront();Activate();Tick();}
        public void OpenSettings(){
            SettingsForm existing=OwnedForms.OfType<SettingsForm>().FirstOrDefault();if(existing!=null){existing.Activate();return;}
            using(SettingsForm page=new SettingsForm(Config,Preview))if(page.ShowDialog(this)==DialogResult.OK){Store.Restore(Config,page.Saved);TopMost=Config.TopMost;ReloadBackground();Tick();}
        }
        void ReloadBackground(){
            if(wallpaper!=null){wallpaper.Dispose();wallpaper=null;}
            if(!String.IsNullOrEmpty(Config.BackgroundImage))try{
                string path=Path.IsPathRooted(Config.BackgroundImage)?Config.BackgroundImage:Path.Combine(Store.Folder,Config.BackgroundImage);
                using(Image original=Image.FromFile(path))wallpaper=new Bitmap(original);
            }catch{wallpaper=null;}
        }
        protected override void OnPaint(PaintEventArgs e){
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            Color bg=ColorTranslator.FromHtml(Config.BackgroundColor);g.Clear(bg);
            if(wallpaper!=null){
                float sx=(float)Width/wallpaper.Width,sy=(float)Height/wallpaper.Height;
                if(Config.BackgroundFit=="stretch")g.DrawImage(wallpaper,new Rectangle(0,0,Width,Height));
                else{float scale=Config.BackgroundFit=="contain"?Math.Min(sx,sy):Math.Max(sx,sy);float w=wallpaper.Width*scale,h=wallpaper.Height*scale;g.DrawImage(wallpaper,(Width-w)/2,(Height-h)/2,w,h);}
                using(Brush tint=new SolidBrush(Color.FromArgb(Config.PanelOpacity*255/100,bg)))g.FillRectangle(tint,ClientRectangle);
            }
            using(GraphicsPath p=Art.Round(new Rectangle(0,0,Width-1,Height-1),18))using(Pen pen=new Pen(Color.FromArgb(219,229,222)))g.DrawPath(pen,p);
            if(wallpaper!=null||bg.GetBrightness()<.75f){
                using(Brush wash=new SolidBrush(Color.FromArgb(235,Color.White))){
                    using(GraphicsPath p=Art.Round(new Rectangle(7,7,216,49),10))g.FillPath(wash,p);
                    using(GraphicsPath p=Art.Round(new Rectangle(7,148,216,41),10))g.FillPath(wash,p);
                }
            }
            Result r=Current;if(r==null)return;
            Art.Text(g,"本月累计 · "+r.Now.ToString("MM月"),10,Art.Muted,14,9);
            string month="¥ "+r.Month.ToString("F2",CultureInfo.InvariantCulture);float monthSize=month.Length>11?16:21;
            Art.Text(g,month,monthSize,Art.Ink,12,29,"Consolas");Art.Cat(g,177,32,2.6f,r.Now.Second,Mood);
            Rectangle lcd=new Rectangle(12,64,206,78);
            using(GraphicsPath p=Art.Round(lcd,12))using(Brush b=new SolidBrush(Color.FromArgb(238,246,247,243)))g.FillPath(b,p);
            Art.Text(g,"今日已赚",10,Art.Muted,25,69);Art.Text(g,"CNY",8,Art.Muted,184,71,"Consolas");
            Art.Digits(g,DisplayedToday.ToString("F"+Config.Decimals,CultureInfo.InvariantCulture),new RectangleF(24,90,182,30),Art.Ink);
            string weekday=new string[]{"日","一","二","三","四","五","六"}[(int)r.Now.DayOfWeek];
            Art.Text(g,r.Now.ToString("MM.dd")+" 周"+weekday,8.5f,Art.Muted,25,130);
            Art.Digits(g,r.Now.ToString("HH:mm:ss"),new RectangleF(96,129,73,10),Art.Green);
            Art.Text(g,"UTC+8",7.5f,Art.Muted,177,131,"Consolas");
            using(Brush b=new SolidBrush(r.ActiveOvertime||r.State.StartsWith("工作中")?Art.Green:Color.FromArgb(146,160,154)))g.FillEllipse(b,16,152,4,4);
            Art.Text(g,ReactionText,9.2f,Art.Ink,27,146);
        }
    }
    public class OvertimeAskForm:WhiteForm {
        public OvertimeAskForm(Config c){
            Text="电子猫 · 今日加班询问";ClientSize=new Size(370,217);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;TopMost=true;StartPosition=FormStartPosition.CenterScreen;
            LabelAt(this,"今天要加班吗？",25,29,280,35,17f);
            string time=c.OvertimeStart;string message="选“是”：从 "+time+" 开始计加班与当日餐补。\n选“否”或关闭：今天不自动计加班。";
            if(Payroll.ChinaNow().TimeOfDay>Payroll.Time(time))message+="\n现在已过起点，选“是”会从 "+time+" 追计。";
            LabelAt(this,message,25,76,323,76,9f);
            PetButton yes=ButtonAt(this,"是，要加班",25,164,155),no=ButtonAt(this,"否，今天不加班",190,164,155);
            yes.DialogResult=DialogResult.Yes;no.DialogResult=DialogResult.No;AcceptButton=no;CancelButton=no;
        }
    }
    public class SettingsForm:WhiteForm {
        Config original,edit;bool preview;public Config Saved;
        TabControl tabs;NumericUpDown salary,fixedDays,companyDays,otRate,meal,opacity,decimals;
        DateTimePicker hire,month,workStart,workEnd,lunchStart,lunchEnd,prompt,otStart,anchor;
        ComboBox mode,schedule,singleRest,doubleRest1,doubleRest2,fit,anchorKind;
        CheckBox cap,lunch,automatic,ask,topmost,startup,closeTray,moneyBar;
        TextBox backgroundPath;Button colorButton;string selectedColor;Label rate,calendarInfo;
        DataGridView manualGrid,sessionGrid,calendarGrid;DateTimePicker calendarDate;ComboBox calendarKind;TextBox calendarNote;
        Dictionary<string,decimal> monthDays;bool changingMonth;string selectedMonth;
        NumericUpDown attendanceBonus;CheckBox attendanceEnabled,attendanceProrate,attendanceLeave,attendanceLate;
        ComboBox attendanceMode,attendanceStatus;DateTimePicker attendanceMonth;DataGridView attendanceGrid;
        Dictionary<string,string> attendanceStates;string attendanceSelectedMonth;bool attendanceLoading;
        public SettingsForm(Config c,bool noSideEffects=false){
            original=c;edit=Store.Clone(c);preview=noSideEffects;monthDays=new Dictionary<string,decimal>(c.CompanyDays);selectedColor=c.BackgroundColor;
            Text="工资桌宠 · 设置";ClientSize=new Size(640,655);MinimumSize=new Size(656,580);FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=false;MinimizeBox=false;
            LabelAt(this,"设置",18,14,350,30,16f);LabelAt(this,"金额与记录仅保存在本机；修改规则后会重算历史估算。",18,49,600,24,9f);
            tabs=new TabControl{Location=new Point(15,81),Size=new Size(610,519),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};Controls.Add(tabs);
            TabPage pay=Page("计薪"),time=Page("作息"),overtime=Page("加班"),calendar=Page("日历"),appearance=Page("外观"),system=Page("系统"),attendance=Page("全勤");
            BuildPay(pay);BuildTime(time);BuildOvertime(overtime);BuildCalendar(calendar);BuildAppearance(appearance);BuildSystem(system);BuildAttendance(attendance);
            PetButton save=ButtonAt(this,"保存并返回",482,613,143,30),cancel=ButtonAt(this,"取消",381,613,90,30);
            save.Anchor=cancel.Anchor=AnchorStyles.Right|AnchorStyles.Bottom;save.Click+=delegate{SaveAll();};cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};CancelButton=cancel;
            PetButton details=ButtonAt(this,"工资明细 / 导出",15,613,155,30);details.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
            details.Click+=delegate{using(DetailForm detail=new DetailForm(original))detail.ShowDialog(this);};
            LoadMonth();UpdateRate();
        }
        TabPage Page(string name){TabPage p=new TabPage(name){BackColor=Color.White,AutoScroll=true};tabs.TabPages.Add(p);return p;}
        NumericUpDown Number(Control p,string label,int y,decimal value,decimal min,decimal max,int digits=2){LabelAt(p,label,16,y+5,218);NumericUpDown n=new NumericUpDown{Location=new Point(239,y),Size=new Size(320,27),Minimum=min,Maximum=max,DecimalPlaces=digits,Increment=digits==0?1:.5m,Value=Math.Max(min,Math.Min(max,value))};p.Controls.Add(n);return n;}
        DateTimePicker DateField(Control p,string label,int y,string value){LabelAt(p,label,16,y+5,218);DateTimePicker d=new DateTimePicker{Location=new Point(239,y),Size=new Size(320,27),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM-dd",Value=Payroll.Date(value)};p.Controls.Add(d);return d;}
        DateTimePicker TimeField(Control p,string label,int y,string value){DateTimePicker d=DateField(p,label,y,"2026-01-01");d.CustomFormat="HH:mm";d.ShowUpDown=true;d.Value=d.Value.Date+Payroll.Time(value);return d;}
        ComboBox Choice(Control p,string label,int y,string[] values,int index){
            LabelAt(p,label,16,y+5,218);ComboBox b=new ComboBox{Location=new Point(239,y),Size=new Size(320,28),DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Art.Ink,BackColor=Color.White};
            b.Items.AddRange(values);b.SelectedIndex=index;
            p.Controls.Add(b);return b;
        }
        CheckBox Check(Control p,string text,int y,bool value){CheckBox b=new CheckBox{Text=text,Checked=value,Location=new Point(18,y),Size=new Size(555,28)};p.Controls.Add(b);return b;}
        void BuildPay(Control p){
            salary=Number(p,"月底薪（元）",18,edit.MonthlySalary,.01m,10000000m);hire=DateField(p,"入职日期",57,edit.HireDate);
            mode=Choice(p,"底薪折算方式",96,new[]{"固定天数折算（默认21.75天）","当月应出勤天数折算"},edit.Mode=="fixed"?0:1);
            fixedDays=Number(p,"固定折算天数",135,edit.FixedDays,.01m,31m);cap=Check(p,"底薪月累计不超过设置的月薪（加班与餐补另计）",174,edit.CapBase);
            month=DateField(p,"编辑应出勤天数的月份",216,Payroll.ChinaNow().ToString("yyyy-MM-dd"));month.CustomFormat="yyyy-MM";
            automatic=Check(p,"该月自动按排班与日历计算应出勤天数",255,true);
            companyDays=Number(p,"该月公司应出勤天数",294,24m,.01m,31m);
            rate=LabelAt(p,"",18,350,550,65,11f);
            LabelAt(p,"自动模式会纳入日历中的休息/假期和调休上班。\n请假日仍属于应出勤日，但当天不累计底薪。",18,424,555,55,9f);
            mode.SelectedIndexChanged+=delegate{UpdateRate();};salary.ValueChanged+=delegate{UpdateRate();};fixedDays.ValueChanged+=delegate{UpdateRate();};companyDays.ValueChanged+=delegate{if(!changingMonth){KeepMonth();UpdateRate();}};
            automatic.CheckedChanged+=delegate{if(!changingMonth){KeepMonth();UpdateRate();}};
            month.ValueChanged+=delegate{LoadMonth();};
        }
        void KeepMonth(){if(selectedMonth==null)return;if(automatic.Checked)monthDays.Remove(selectedMonth);else monthDays[selectedMonth]=companyDays.Value;}
        void LoadMonth(){
            changingMonth=true;selectedMonth=month.Value.ToString("yyyy-MM");decimal val;
            automatic.Checked=!monthDays.TryGetValue(selectedMonth,out val);
            companyDays.Value=Math.Max(.01m,automatic.Checked?Payroll.CalendarDays(edit,month.Value):val);
            changingMonth=false;UpdateRate();
        }
        void UpdateRate(){
            if(rate==null||workStart==null)return;
            bool monthly=mode.SelectedIndex==1;companyDays.Enabled=monthly&&!automatic.Checked;automatic.Enabled=monthly;fixedDays.Enabled=!monthly;
            decimal divisor;
            try{
                Config previewRules=Store.Clone(edit);
                if(schedule!=null){previewRules.Schedule=new[]{"alternate","single","double"}[schedule.SelectedIndex];previewRules.DoubleWeekMonday=anchor.Value.ToString("yyyy-MM-dd");previewRules.AnchorIsDouble=anchorKind.SelectedIndex==0;previewRules.SingleRestDay=singleRest.SelectedIndex;previewRules.DoubleRestDay1=doubleRest1.SelectedIndex;previewRules.DoubleRestDay2=doubleRest2.SelectedIndex;}
                if(calendarGrid!=null){previewRules.Calendar=new List<DayRule>();foreach(DataGridViewRow row in calendarGrid.Rows){if(row.IsNewRow)continue;previewRules.Calendar.Add(new DayRule{Date=Convert.ToString(row.Cells[0].Value),Kind=Convert.ToString(row.Cells[1].Value)});}}
                decimal count=Payroll.CalendarDays(previewRules,month.Value);
                if(automatic.Checked){changingMonth=true;companyDays.Value=Math.Max(.01m,count);changingMonth=false;}
                divisor=monthly?(automatic.Checked?count:companyDays.Value):fixedDays.Value;
            }catch{changingMonth=false;rate.Text="请检查日历日期与排班设置。";return;}
            decimal seconds=(decimal)(workEnd.Value.TimeOfDay-workStart.Value.TimeOfDay).TotalSeconds;
            if(lunch.Checked)seconds-=(decimal)(lunchEnd.Value.TimeOfDay-lunchStart.Value.TimeOfDay).TotalSeconds;
            rate.Text=divisor>0&&seconds>0?String.Format("日薪 ¥{0:F2}   工作时薪 ¥{1:F2}\n每日有效工作时长 {2:0.##} 小时",salary.Value/divisor,salary.Value/divisor/(seconds/3600m),seconds/3600m):"请检查工作时段和应出勤天数。";
        }
        static string[] Weekdays={"周日","周一","周二","周三","周四","周五","周六"};
        void BuildTime(Control p){
            workStart=TimeField(p,"上班时间",15,edit.WorkStart);workEnd=TimeField(p,"下班时间",53,edit.WorkEnd);
            lunch=Check(p,"午休不计薪",91,edit.LunchEnabled);lunchStart=TimeField(p,"午休开始",129,edit.LunchStart);lunchEnd=TimeField(p,"午休结束",167,edit.LunchEnd);
            schedule=Choice(p,"排班方式",205,new[]{"单双休交替","固定单休","固定双休"},edit.Schedule=="alternate"?0:edit.Schedule=="single"?1:2);
            anchor=DateField(p,"单双休锚点（必须是周一）",243,edit.DoubleWeekMonday);
            anchorKind=Choice(p,"锚点周类型",281,new[]{"双休周","单休周"},edit.AnchorIsDouble?0:1);
            singleRest=Choice(p,"单休周休息日",319,Weekdays,edit.SingleRestDay);
            doubleRest1=Choice(p,"双休周休息日一",357,Weekdays,edit.DoubleRestDay1);doubleRest2=Choice(p,"双休周休息日二",395,Weekdays,edit.DoubleRestDay2);
            LabelAt(p,"固定采用东八区（UTC+8）。假期、请假、调休可在日历页覆盖。",18,446,560,38,9f);
            foreach(DateTimePicker d in new[]{workStart,workEnd,lunchStart,lunchEnd})d.ValueChanged+=delegate{UpdateRate();};lunch.CheckedChanged+=delegate{UpdateRate();};
            foreach(ComboBox b in new[]{schedule,anchorKind,singleRest,doubleRest1,doubleRest2})b.SelectedIndexChanged+=delegate{UpdateRate();};anchor.ValueChanged+=delegate{UpdateRate();};
        }
        DataGridView Grid(Control p,int y,int h){
            DataGridView grid=new DataGridView{Location=new Point(16,y),Size=new Size(555,h),BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,RowHeadersVisible=false,AllowUserToAddRows=true,AllowUserToDeleteRows=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false};
            grid.DefaultCellStyle.Font=new Font("Microsoft YaHei UI",8.5f);grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(239,245,241);grid.EnableHeadersVisualStyles=false;
            grid.DataError+=delegate(object sender,DataGridViewDataErrorEventArgs e){e.ThrowException=false;};p.Controls.Add(grid);return grid;
        }
        void BuildOvertime(Control p){
            otRate=Number(p,"加班费（元/小时）",15,edit.OvertimeRate,0,100000m);
            meal=Number(p,"餐补（元/加班日）",53,edit.MealAllowance,0,100000m);
            ask=Check(p,"工作日到点询问是否加班（每天最多一次）",91,edit.AskOvertime);
            prompt=TimeField(p,"加班询问时间",129,edit.PromptTime);otStart=TimeField(p,"加班开始计薪时间",167,edit.OvertimeStart);
            LabelAt(p,"手动补录：十进制小时。选中行按 Delete 可删除。",18,213,560,24,9f);
            manualGrid=Grid(p,243,150);manualGrid.Columns.Add("Date","日期 yyyy-MM-dd");manualGrid.Columns.Add("Hours","已完成加班小时");
            foreach(var o in edit.Overtime)manualGrid.Rows.Add(o.Date,o.Hours.ToString("0.####",CultureInfo.InvariantCulture));
            manualGrid.DefaultValuesNeeded+=delegate(object sender,DataGridViewRowEventArgs e){e.Row.Cells[0].Value=Payroll.ChinaNow().ToString("yyyy-MM-dd");e.Row.Cells[1].Value="0";};
            LabelAt(p,"计时记录：起止为东八区；结束留空会计到结束操作或24:00。",18,413,560,42,9f);
            sessionGrid=Grid(p,459,150);sessionGrid.AllowUserToAddRows=false;
            sessionGrid.Columns.Add("Date","日期");sessionGrid.Columns.Add("Start","开始 yyyy-MM-ddTHH:mm:ss");sessionGrid.Columns.Add("End","结束（可留空）");
            foreach(var s in edit.Sessions)sessionGrid.Rows.Add(s.Date,s.Start,s.End);
            LabelAt(p,"补录与计时记录会相加，避免重复录入；同日餐补仅计一次。\n新增补录不允许未来日期；开始/结束计时请用主界面“加班”。",18,630,560,62,9f);
        }
        void BuildCalendar(Control p){
            calendarDate=DateField(p,"日期",18,Payroll.ChinaNow().ToString("yyyy-MM-dd"));
            calendarKind=Choice(p,"当天类型",57,new[]{"上班 / 调休上班","休息 / 节假日","请假（不计底薪）"},0);
            LabelAt(p,"备注",16,104,215);calendarNote=new TextBox{Location=new Point(239,97),Size=new Size(320,27)};p.Controls.Add(calendarNote);
            PetButton add=ButtonAt(p,"添加 / 更新该日",380,136,179);add.Click+=delegate{
                string date=calendarDate.Value.ToString("yyyy-MM-dd"),kind=new[]{"work","rest","leave"}[calendarKind.SelectedIndex];
                DataGridViewRow existing=calendarGrid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r=>!r.IsNewRow&&Convert.ToString(r.Cells[0].Value)==date);
                if(existing==null)calendarGrid.Rows.Add(date,kind,calendarNote.Text);else{existing.Cells[1].Value=kind;existing.Cells[2].Value=calendarNote.Text;}
                calendarInfo.Text="已更新 "+date+"。保存后立即应用到排班和工资。";
            };
            LabelAt(p,"日历覆盖：选中行按 Delete 删除，删除后恢复默认排班。",18,185,560,30,9f);
            calendarGrid=Grid(p,219,203);calendarGrid.AllowUserToAddRows=false;
            calendarGrid.Columns.Add("Date","日期");DataGridViewComboBoxColumn kinds=new DataGridViewComboBoxColumn{Name="Kind",HeaderText="类型",DataSource=new[]{new KindItem("work","上班/调休上班"),new KindItem("rest","休息/假期"),new KindItem("leave","请假")},DisplayMember="Name",ValueMember="Value"};calendarGrid.Columns.Add(kinds);calendarGrid.Columns.Add("Note","备注");
            foreach(var d in edit.Calendar)calendarGrid.Rows.Add(d.Date,d.Kind,d.Note);
            calendarGrid.CellValueChanged+=delegate{UpdateRate();};calendarGrid.RowsRemoved+=delegate{UpdateRate();};
            calendarInfo=LabelAt(p,"休息/假期不计底薪；请假仍计入应出勤天数，但当天不计底薪。\n日历只改变底薪与加班询问资格，不删除历史加班记录。",18,440,560,66,9f);
        }
        public class KindItem {public string Value{get;set;}public string Name{get;set;}public KindItem(string value,string name){Value=value;Name=name;}}
        void BuildAppearance(Control p){
            LabelAt(p,"背景颜色",16,24,210);colorButton=ButtonAt(p,"选择颜色",239,18,320);colorButton.BackColor=ColorTranslator.FromHtml(selectedColor);
            colorButton.Click+=delegate{using(ColorDialog d=new ColorDialog{Color=ColorTranslator.FromHtml(selectedColor),FullOpen=true})if(d.ShowDialog(this)==DialogResult.OK){selectedColor=ColorTranslator.ToHtml(d.Color);colorButton.BackColor=d.Color;}};
            LabelAt(p,"背景图片",16,71,210);backgroundPath=new TextBox{Location=new Point(16,103),Size=new Size(543,27),ReadOnly=true,Text=edit.BackgroundImage};p.Controls.Add(backgroundPath);
            PetButton choose=ButtonAt(p,"选择图片…",16,146,145),clear=ButtonAt(p,"清除图片",172,146,145);
            choose.Click+=delegate{using(OpenFileDialog d=new OpenFileDialog{Title="选择主界面背景图片",Filter="图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif"})if(d.ShowDialog(this)==DialogResult.OK){try{using(Image image=Image.FromFile(d.FileName)){if(image.Width>16000||image.Height>16000)throw new Exception("图片尺寸过大，请使用16000像素以内的图片。");}backgroundPath.Text=d.FileName;}catch(Exception ex){MessageBox.Show(ex.Message,"图片无法使用");}}};
            clear.Click+=delegate{backgroundPath.Text="";};
            fit=Choice(p,"图片显示方式",198,new[]{"填满并裁切","完整显示","拉伸"},edit.BackgroundFit=="contain"?1:edit.BackgroundFit=="stretch"?2:0);
            opacity=Number(p,"背景浅色遮罩（%）",244,edit.PanelOpacity,20,100,0);decimals=Number(p,"今日金额的小数位",290,edit.Decimals,2,4,0);
            LabelAt(p,"主界面230×193像素，宽高都是第一版的一半，以白色为主。\n不会显示底薪、加班单价、餐补单价等计薪参数。\n所选背景会复制到程序文件夹，移动后仍可使用。",18,345,560,82,9f);
        }
        void BuildSystem(Control p){
            topmost=Check(p,"桌宠始终置顶",19,edit.TopMost);startup=Check(p,"随 Windows 登录启动",62,edit.Startup);closeTray=Check(p,"点击 × 收起到系统托盘（托盘菜单可以退出）",105,edit.CloseToTray);
            moneyBar=Check(p,"最小化时在任务栏右侧显示金额条（双击恢复桌宠）",148,edit.TaskbarMoneyBar);
            LabelAt(p,"最小化：任务栏右侧金额条每秒更新，避开托盘图标。\n任务栏按钮标题同时保留今日金额。\nWindows 从不合并按钮时才直接显示按钮标题。\n\n托盘：双击恢复；右键设置、明细、加班或退出。\n开机启动使用当前路径，移动后需重新保存选项。\n时区固定东八区 UTC+8；所有数据保存在本地。",18,202,560,165,9f);
            PetButton reset=ButtonAt(p,"重置窗口位置",18,384,175);reset.Click+=delegate{edit.Left=-1;edit.Top=-1;MessageBox.Show("保存后，重新打开程序会回到屏幕右下角。");};
        }
        void BuildAttendance(Control p){
            attendanceStates=new Dictionary<string,string>(edit.AttendanceStatus);
            attendanceEnabled=Check(p,"启用全勤奖（与底薪、加班分开计算）",15,edit.AttendanceEnabled);
            attendanceBonus=Number(p,"每月全勤奖（元）",57,edit.AttendanceBonus,0,10000000m);
            attendanceMode=Choice(p,"全勤计入方式",96,new[]{"月底达标后一次计入","按出勤时间逐日累计"},edit.AttendanceMode=="daily"?1:0);
            attendanceProrate=Check(p,"入职首月全勤按入职后应出勤天数折算",135,edit.AttendanceProrateHire);
            attendanceLeave=Check(p,"请假取消该月全勤（自动判断时生效）",173,edit.AttendanceCancelLeave);
            attendanceLate=Check(p,"迟到取消该月全勤（自动判断时生效）",211,edit.AttendanceCancelLate);
            attendanceMonth=DateField(p,"编辑全勤资格月份",251,Payroll.ChinaNow().ToString("yyyy-MM-dd"));attendanceMonth.CustomFormat="yyyy-MM";
            attendanceStatus=Choice(p,"该月全勤资格",290,new[]{"待确认：暂不计入已赚","按已录考勤自动判断","公司确认有全勤（覆盖异常）","无全勤"},1);
            attendanceStatus.SelectedIndexChanged+=delegate{if(!attendanceLoading)KeepAttendanceMonth();};
            attendanceMonth.ValueChanged+=delegate{KeepAttendanceMonth();LoadAttendanceMonth();};LoadAttendanceMonth();
            LabelAt(p,"考勤异常：迟到或手动取消全勤；请假在日历页登记；Delete 删除。",18,331,555,42,9f);
            attendanceGrid=Grid(p,373,145);attendanceGrid.Columns.Add("Date","日期 yyyy-MM-dd");
            DataGridViewComboBoxColumn kinds=new DataGridViewComboBoxColumn{Name="Kind",HeaderText="异常类型",DataSource=new[]{new KindItem("late","迟到"),new KindItem("other","手动取消全勤")},DisplayMember="Name",ValueMember="Value"};attendanceGrid.Columns.Add(kinds);attendanceGrid.Columns.Add("Note","备注");
            foreach(var issue in edit.AttendanceIssues)attendanceGrid.Rows.Add(issue.Date,issue.Kind,issue.Note);
            attendanceGrid.DefaultValuesNeeded+=delegate(object sender,DataGridViewRowEventArgs e){e.Row.Cells[0].Value=Payroll.ChinaNow().ToString("yyyy-MM-dd");e.Row.Cells[1].Value="late";};
            LabelAt(p,"自动判断仅根据你录入的请假和异常，不读取公司打卡。\n资格待确认时不计入工资；月底模式到该月最后一天的下班时间计入。\n逐日累计属于估算：后续取消资格会移除整月已累计全勤。\n首月折算使用已选计薪分母；不折算则按设置的金额计算。",18,532,560,98,9f);
        }
        void KeepAttendanceMonth(){if(attendanceSelectedMonth!=null&&!attendanceLoading)attendanceStates[attendanceSelectedMonth]=new[]{"pending","auto","yes","no"}[attendanceStatus.SelectedIndex];}
        void LoadAttendanceMonth(){attendanceLoading=true;attendanceSelectedMonth=attendanceMonth.Value.ToString("yyyy-MM");string value;if(!attendanceStates.TryGetValue(attendanceSelectedMonth,out value))value="auto";attendanceStatus.SelectedIndex=Array.IndexOf(new[]{"pending","auto","yes","no"},value);attendanceLoading=false;}
        decimal ParseDecimal(object value){return decimal.Parse(Convert.ToString(value),NumberStyles.Number,CultureInfo.InvariantCulture);}
        Config Collect(){
            KeepMonth();Config c=Store.Clone(edit);c.MonthlySalary=salary.Value;c.HireDate=hire.Value.ToString("yyyy-MM-dd");c.Mode=mode.SelectedIndex==0?"fixed":"calendar";c.FixedDays=fixedDays.Value;c.CapBase=cap.Checked;c.CompanyDays=new Dictionary<string,decimal>(monthDays);
            c.WorkStart=workStart.Value.ToString("HH:mm");c.WorkEnd=workEnd.Value.ToString("HH:mm");c.LunchEnabled=lunch.Checked;c.LunchStart=lunchStart.Value.ToString("HH:mm");c.LunchEnd=lunchEnd.Value.ToString("HH:mm");
            c.Schedule=new[]{"alternate","single","double"}[schedule.SelectedIndex];c.DoubleWeekMonday=anchor.Value.ToString("yyyy-MM-dd");c.AnchorIsDouble=anchorKind.SelectedIndex==0;
            c.SingleRestDay=singleRest.SelectedIndex;c.DoubleRestDay1=doubleRest1.SelectedIndex;c.DoubleRestDay2=doubleRest2.SelectedIndex;
            c.OvertimeRate=otRate.Value;c.MealAllowance=meal.Value;c.PromptTime=prompt.Value.ToString("HH:mm");c.OvertimeStart=otStart.Value.ToString("HH:mm");c.AskOvertime=ask.Checked;
            manualGrid.EndEdit();sessionGrid.EndEdit();calendarGrid.EndEdit();
            c.Overtime=new List<Overtime>();foreach(DataGridViewRow row in manualGrid.Rows){if(row.IsNewRow)continue;string date=Convert.ToString(row.Cells[0].Value);decimal h=ParseDecimal(row.Cells[1].Value);if(Payroll.Date(date)>Payroll.ChinaNow().Date)throw new Exception("手动补录加班不能使用未来日期。");c.Overtime.Add(new Overtime{Date=date,Hours=h});}
            c.Sessions=new List<Session>();foreach(DataGridViewRow row in sessionGrid.Rows){if(row.IsNewRow)continue;c.Sessions.Add(new Session{Date=Convert.ToString(row.Cells[0].Value),Start=Convert.ToString(row.Cells[1].Value),End=Convert.ToString(row.Cells[2].Value)});}
            c.Calendar=new List<DayRule>();foreach(DataGridViewRow row in calendarGrid.Rows){if(row.IsNewRow)continue;c.Calendar.Add(new DayRule{Date=Convert.ToString(row.Cells[0].Value),Kind=Convert.ToString(row.Cells[1].Value),Note=Convert.ToString(row.Cells[2].Value)});}
            c.BackgroundColor=selectedColor;c.BackgroundImage=backgroundPath.Text;c.BackgroundFit=new[]{"cover","contain","stretch"}[fit.SelectedIndex];c.PanelOpacity=(int)opacity.Value;c.Decimals=(int)decimals.Value;
            c.TopMost=topmost.Checked;c.Startup=startup.Checked;c.CloseToTray=closeTray.Checked;c.TaskbarMoneyBar=moneyBar.Checked;
            KeepAttendanceMonth();c.AttendanceEnabled=attendanceEnabled.Checked;c.AttendanceBonus=attendanceBonus.Value;c.AttendanceMode=attendanceMode.SelectedIndex==1?"daily":"monthly";c.AttendanceProrateHire=attendanceProrate.Checked;c.AttendanceCancelLeave=attendanceLeave.Checked;c.AttendanceCancelLate=attendanceLate.Checked;c.AttendanceStatus=new Dictionary<string,string>(attendanceStates);
            attendanceGrid.EndEdit();c.AttendanceIssues=new List<AttendanceIssue>();foreach(DataGridViewRow row in attendanceGrid.Rows){if(row.IsNewRow)continue;c.AttendanceIssues.Add(new AttendanceIssue{Date=Convert.ToString(row.Cells[0].Value),Kind=Convert.ToString(row.Cells[1].Value),Note=Convert.ToString(row.Cells[2].Value)});}
            // Preserve a choice made by the clock timer while settings are open.
            c.Decisions=new Dictionary<string,string>(original.Decisions);
            foreach(Session fresh in original.Sessions)if(!edit.Sessions.Any(s=>s.Date==fresh.Date&&s.Start==fresh.Start))c.Sessions.Add(fresh);
            Payroll.Validate(c);return c;
        }
        public void SaveAll(){
            try{
                Config c=Collect();
                if(!String.IsNullOrEmpty(c.BackgroundImage)&&Path.IsPathRooted(c.BackgroundImage)){
                    string folder=Path.Combine(Store.Folder,"背景");Directory.CreateDirectory(folder);string filename="background-"+Guid.NewGuid().ToString("N")+".png";
                    using(Image image=Image.FromFile(c.BackgroundImage))image.Save(Path.Combine(folder,filename),ImageFormat.Png);c.BackgroundImage=Path.Combine("背景",filename);
                }
                if(!preview)StartupManager.Apply(c.Startup);
                try{Store.Save(c);}catch{if(!preview)StartupManager.Apply(original.Startup);throw;}
                Saved=c;DialogResult=DialogResult.OK;Close();
            }catch(Exception ex){MessageBox.Show(this,"设置未保存："+ex.Message,"请检查输入",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        }
        public void SelectTab(int index){tabs.SelectedIndex=index;}
        public void VerifyEditing(string testBackground=null){
            mode.SelectedIndex=0;if(!rate.Text.Contains((salary.Value/fixedDays.Value).ToString("F2")))throw new Exception("固定天数预览失败");
            mode.SelectedIndex=1;automatic.Checked=true;UpdateRate();if(!rate.Text.Contains((salary.Value/Payroll.CalendarDays(Collect(),month.Value)).ToString("F2")))throw new Exception("日历天数预览失败");
            automatic.Checked=false;companyDays.Value=22m;if(!rate.Text.Contains((salary.Value/22m).ToString("F2")))throw new Exception("公司天数预览失败");
            Config collected=Collect();if(collected.CompanyDays[Payroll.ChinaNow().ToString("yyyy-MM")]!=22m)throw new Exception("公司天数收集失败");
            attendanceEnabled.Checked=true;attendanceBonus.Value=600m;attendanceMode.SelectedIndex=1;attendanceProrate.Checked=true;attendanceStatus.SelectedIndex=1;
            collected=Collect();if(collected.AttendanceBonus!=600m||collected.AttendanceMode!="daily"||!collected.AttendanceProrateHire||collected.AttendanceStatus[Payroll.ChinaNow().ToString("yyyy-MM")]!="auto")throw new Exception("全勤设置收集失败");
            if(testBackground!=null){backgroundPath.Text=testBackground;fit.SelectedIndex=1;opacity.Value=40;}
        }
    }
    public class DetailForm:WhiteForm {
        Config config;DateTimePicker month;DataGridView table;Label totals;Result result;Timer refresh;
        public DetailForm(Config c){
            config=c;Text="工资桌宠 · 每日明细";ClientSize=new Size(890,544);MinimumSize=new Size(800,440);
            LabelAt(this,"每日薪资明细",18,16,300,30,16f);
            month=new DateTimePicker{Location=new Point(18,59),Size=new Size(165,28),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy-MM",Value=Payroll.ChinaNow()};Controls.Add(month);
            PetButton export=ButtonAt(this,"导出本月CSV",720,57,151);export.Anchor=AnchorStyles.Top|AnchorStyles.Right;
            table=new DataGridView{Location=new Point(18,108),Size=new Size(854,328),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom,ReadOnly=true,AllowUserToAddRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White};
            foreach(string s in new[]{"日期","类型","普通工时","底薪","全勤奖","加班小时","加班费","餐补","当日合计"})table.Columns.Add(s,s);Controls.Add(table);
            totals=LabelAt(this,"",18,454,850,67,10f);totals.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
            export.Click+=delegate{using(SaveFileDialog d=new SaveFileDialog{Title="导出工资明细",Filter="CSV文件|*.csv",FileName="工资明细-"+month.Value.ToString("yyyy-MM")+".csv"})if(d.ShowDialog(this)==DialogResult.OK){Reload();File.WriteAllText(d.FileName,Reports.Csv(result),new System.Text.UTF8Encoding(true));MessageBox.Show("已导出到：\n"+d.FileName,"导出完成");}};
            month.ValueChanged+=delegate{Reload();};refresh=new Timer{Interval=1000};refresh.Tick+=delegate{Reload();};Shown+=delegate{refresh.Start();};FormClosed+=delegate{refresh.Stop();refresh.Dispose();};Reload();
        }
        void Reload(){
            DateTime now=Payroll.ChinaNow(),m=month.Value;DateTime at=m.Year==now.Year&&m.Month==now.Month?now:new DateTime(m.Year,m.Month,DateTime.DaysInMonth(m.Year,m.Month),23,59,59);
            if(at>now)at=new DateTime(m.Year,m.Month,1).AddTicks(-1);
            if(at.Year!=m.Year||at.Month!=m.Month){table.Rows.Clear();totals.Text="未来月份尚无已赚收入。";result=new Result{Now=new DateTime(m.Year,m.Month,1),Days=new List<DayPay>()};return;}
            result=Payroll.Calculate(config,at);int selected=table.CurrentCell==null?-1:table.CurrentCell.RowIndex;
            int scroll=table.FirstDisplayedScrollingRowIndex;
            table.Rows.Clear();foreach(DayPay d in result.Days.Where(d=>d.Date<=at.Date))table.Rows.Add(d.Date.ToString("yyyy-MM-dd"),d.Type,d.WorkHours.ToString("0.####"),d.Base.ToString("F2"),d.Attendance.ToString("F2"),d.OvertimeHours.ToString("0.####"),d.Overtime.ToString("F2"),d.Meal.ToString("F2"),d.Total.ToString("F2"));
            if(selected>=0&&selected<table.Rows.Count)table.CurrentCell=table.Rows[selected].Cells[0];
            if(scroll>=0&&scroll<table.Rows.Count)table.FirstDisplayedScrollingRowIndex=scroll;
            totals.Text=String.Format("底薪 ¥{0:F2}   全勤 ¥{1:F2}   加班 ¥{2:F2}   餐补 ¥{3:F2}   合计 ¥{4:F2}\n全勤：{5}（潜在奖励 ¥{6:F2}）；税前估算，按当前规则重算历史。",result.MonthBase,result.MonthAttendance,result.MonthOvertime,result.MonthMeal,result.Month,result.AttendanceState,result.AttendancePotential);
        }
    }
}

