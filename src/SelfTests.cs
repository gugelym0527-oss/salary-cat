using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SalaryPet {
    sealed class ClickableTestClock:ClockForm {
        public ClickableTestClock(Config c):base(c,true){}
        public void Tap(int clicks,Point release){
            Point head=new Point(190,42);
            OnMouseDown(new MouseEventArgs(MouseButtons.Left,clicks,head.X,head.Y,0));
            OnMouseUp(new MouseEventArgs(MouseButtons.Left,clicks,release.X,release.Y,0));
        }
    }
    public static class SelfTests {
        static List<string> passed=new List<string>();
        static Config Reference(){return new Config{MonthlySalary=6000m,AttendanceBonus=600m,HireDate="2020-10-08",DoubleWeekMonday="2020-10-12",OvertimeRate=21m,MealAllowance=10m,AttendanceEnabled=false,Overtime=new List<Overtime>{new Overtime{Date="2020-10-08",Hours=2m},new Overtime{Date="2020-10-09",Hours=1m},new Overtime{Date="2020-10-10",Hours=3m},new Overtime{Date="2020-10-11",Hours=4m}}};}
        static void Check(bool ok,string name){if(!ok)throw new Exception("测试失败："+name);passed.Add(name);}
        static bool Near(decimal a,decimal b){return Math.Abs(a-b)<0.00000001m;}
        static DateTime At(int d,int h,int m=0,int s=0){return new DateTime(2020,10,d,h,m,s);}
        static Result Pay(Config c,int d,int h,int m=0,int s=0){return Payroll.Calculate(c,At(d,h,m,s));}
        public static void CatUI(string folder){
            Directory.CreateDirectory(folder);
            CatReaction chain=new CatReaction();
            chain.Click(1000,500);chain.Click(1500,500);chain.Click(2000,500);chain.Click(2500,500);
            if(chain.Mood!=CatMood.Angry||chain.Refresh(4499))throw new Exception("四击或两秒边界错误");
            chain.Click(3000,500);if(!chain.Refresh(4500)||chain.Mood!=CatMood.Normal)throw new Exception("生气期间点击延长了归零时间");
            chain.Click(5000,500);chain.Click(5501,500);if(chain.Mood!=CatMood.Smile)throw new Exception("分开的点击被错误合并");
            Config c=new Config{AskOvertime=false,MonthlySalary=6000m,AttendanceEnabled=false,HireDate="2020-01-01"};c.Overtime.Clear();
            string configuration=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(c);
            using(ClickableTestClock clock=new ClickableTestClock(c)){
                clock.Show();Application.DoEvents();
                clock.Current=Payroll.Calculate(c,new DateTime(2020,10,12,17,0,0));
                Art.Snapshot(clock,Path.Combine(folder,"猫猫-正常.png"));
                CatMood[] moods={CatMood.Smile,CatMood.Confused,CatMood.Sulky,CatMood.Angry};
                string[] labels={"微笑","疑惑","小情绪","生气"};
                for(int i=0;i<4;i++){
                    clock.Tap(i%2+1,new Point(190,42));Application.DoEvents();
                    if(clock.Mood!=moods[i])throw new Exception("真实鼠标抬起未计入第"+(i+1)+"击");
                    clock.Current=Payroll.Calculate(c,new DateTime(2020,10,12,17,0,0));
                    if((i==3&&clock.DisplayedToday!=0m)||(i<3&&clock.DisplayedToday!=clock.Current.Today.Total))throw new Exception("互动金额显示错误");
                    Art.Snapshot(clock,Path.Combine(folder,"猫猫-"+labels[i]+".png"));
                }
                decimal realToday=clock.Current.Today.Total,realMonth=clock.Current.Month;
                if(realToday<=0m)throw new Exception("未使用正收入验证临时清零");
                clock.WindowState=FormWindowState.Minimized;
                if(clock.Text!="¥0.00"||clock.MoneyBar.Text!="¥0.00")throw new Exception("生气归零未同步任务栏");
                if(clock.Current.Today.Total!=realToday||clock.Current.Month!=realMonth||configuration!=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(c))throw new Exception("互动修改了工资记录");
                clock.Tap(1,new Point(190,42));
                System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
                while(clock.Mood!=CatMood.Normal&&elapsed.ElapsedMilliseconds<2400){Application.DoEvents();Thread.Sleep(10);}
                if(clock.Mood!=CatMood.Normal||clock.DisplayedToday!=clock.Current.Today.Total||clock.Text!="¥"+clock.Current.Today.Total.ToString("F2",CultureInfo.InvariantCulture)||clock.MoneyBar.Text!=clock.Text)throw new Exception("两秒后未自动恢复真实金额");
                clock.WindowState=FormWindowState.Normal;
                clock.Tap(1,new Point(140,42));if(clock.Mood!=CatMood.Normal)throw new Exception("拖出猫头仍触发了点击");
                clock.Tap(1,new Point(190,42));if(clock.Mood!=CatMood.Smile)throw new Exception("恢复后不能开始新一轮点击");
                clock.Close();
            }
            File.WriteAllText(Path.Combine(folder,"猫猫互动验证结果.txt"),"通过：1/2/3/4击表情、双击鼠标事件、连续点击时间边界、四击临时归零、任务栏金额同步、两秒自动恢复、工资与配置不变、拖出取消点击、恢复后重新点击。",new UTF8Encoding(false));
        }
        public static void Settlement(string folder){
            Directory.CreateDirectory(folder);List<string> checks=new List<string>();
            Action<bool,string> verify=delegate(bool ok,string label){if(!ok)throw new Exception(label);checks.Add(label);};
            Config c=new Config{MonthlySalary=6000m,HireDate="2020-10-01",DoubleWeekMonday="2020-10-12",AttendanceEnabled=false,CapBase=false};
            c.Overtime.Clear();c.Calendar.Clear();c.Sessions.Clear();c.Decisions.Clear();
            DateTime at=new DateTime(2020,10,12,16,0,0);Result before=Payroll.Calculate(c,at);
            Payroll.Settle(c,at);Result done=Payroll.Calculate(c,at);decimal day=6000m/21.75m;
            verify(Near(done.Today.Base,day)&&Near(done.Month-before.Month,day-before.Today.Base),"结算立即补满当天底薪，月累计只增加差额");
            verify(!Payroll.ShouldPrompt(c,at.Date.AddHours(19))&&done.State.Contains("已结算"),"结算状态可见，19点不再询问加班");
            Payroll.Settle(c,at.AddMinutes(1));verify(c.EarlyClosures.Count==1&&Near(Payroll.Calculate(c,at.AddHours(7)).Today.Total,done.Today.Total),"重复结算不重复计薪，全天金额不再增长");
            Payroll.UndoSettlement(c,at);verify(Near(Payroll.Calculate(c,at).Today.Base,before.Today.Base)&&Payroll.ShouldPrompt(c,at.Date.AddHours(19)),"撤销恢复实际时间计薪和加班询问");
            c.Mode="calendar";Payroll.Settle(c,at);verify(Near(Payroll.Calculate(c,at).Today.Base,6000m/Payroll.CalendarDays(c,at)),"当月应出勤天数折算也按全天底薪结算");
            c.CapBase=true;c.CompanyDays["2020-10"]=1m;verify(Near(Payroll.Calculate(c,at).MonthBase,6000m),"提前结算仍遵守月薪封顶");
            c.CompanyDays.Clear();c.CapBase=false;Payroll.UndoSettlement(c,at);c.Mode="fixed";c.LunchEnabled=true;Payroll.Settle(c,at);
            verify(Near(Payroll.Calculate(c,at).Today.Base,day),"扣午休模式也补满全天底薪");
            c.LunchEnabled=false;Payroll.UndoSettlement(c,at);Payroll.Decide(c,at.Date.AddHours(19),true);Payroll.Settle(c,at);
            verify(Near(Payroll.Calculate(c,at.Date.AddHours(22)).Today.Meal,0)&&Near(Payroll.Calculate(c,at.Date.AddHours(22)).Today.Overtime,0)&&Payroll.LiveSession(c,at)==null,"尚未开始的加班取消，不虚增加班费或餐补");
            c.Sessions.Clear();c.Decisions.Clear();Payroll.UndoSettlement(c,at);Payroll.Decide(c,at.Date.AddHours(19),true);DateTime late=at.Date.AddHours(21.5);
            Payroll.Settle(c,late);Result paid=Payroll.Calculate(c,late);verify(Near(paid.Today.Overtime,1.5m*c.OvertimeRate)&&Near(paid.Today.Meal,c.MealAllowance)&&Near(Payroll.Calculate(c,at.Date.AddHours(23)).Today.Total,paid.Today.Total),"进行中的加班按已完成时长结算，餐补一次，后续停止增长");
            c.AttendanceEnabled=true;c.AttendanceMode="monthly";verify(Near(Payroll.Calculate(c,late).MonthAttendance,0),"提前下班不提前发放月底全勤奖");
            verify(!Payroll.IsSettled(c,at.AddDays(1)),"次日自动恢复正常计薪");
            bool rejected=false;try{Payroll.Settle(c,new DateTime(2020,10,11,16,0,0));}catch{rejected=true;}verify(rejected,"休息日不能凭空结算底薪");
            c.Calendar.Add(new DayRule{Date="2020-10-13",Kind="leave"});rejected=false;try{Payroll.Settle(c,at.AddDays(1));}catch{rejected=true;}verify(rejected,"请假日不能提前结算底薪");
            string old=Store.PathName;Store.PathName=Path.Combine(folder,"提前下班测试配置.json");
            try{
                Store.Save(c);Config loaded=Store.Load();verify(Near(Payroll.Calculate(loaded,late).Today.Total,Payroll.Calculate(c,late).Today.Total)&&Payroll.IsSettled(loaded,late),"关闭重启后保留结算记录和金额");
                loaded.EarlyClosures=null;File.WriteAllText(Store.PathName,new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(loaded));verify(Store.Load().EarlyClosures!=null,"兼容旧配置中的空结算字段");
                Config ui=new Config{MonthlySalary=6000m,HireDate="2020-01-01",AttendanceEnabled=false,AskOvertime=false,CapBase=false,WorkStart="00:00"};ui.Overtime.Clear();ui.Calendar.Add(new DayRule{Date=Payroll.ChinaNow().ToString("yyyy-MM-dd"),Kind="work"});Store.Save(ui);
                using(ClockForm clock=new ClockForm(ui,true)){
                    clock.Show();Application.DoEvents();Button settle=clock.Controls.OfType<Button>().Single(b=>b.Text=="提前下班");settle.PerformClick();
                    verify(Payroll.IsSettled(ui,Payroll.ChinaNow())&&settle.Text=="撤销结算"&&Near(clock.DisplayedToday,clock.Current.DayRate),"主界面按钮一键结算并显示撤销入口");
                    foreach(Button b in clock.Controls.OfType<Button>().Where(b=>b.Top>140))verify(b.Height>=b.Font.Height+6&&clock.Region.IsVisible(b.Left,b.Top)&&clock.Region.IsVisible(b.Right-1,b.Bottom-1),"底部按钮完整可见："+b.Text);
                    verify(clock.Controls.OfType<Button>().Count(b=>b.Top>140)==3&&!clock.Controls.OfType<Button>().Any(b=>b.Text=="明细"),"主界面仅显示设置、加班、提前下班");
                    Art.Snapshot(clock,Path.Combine(folder,"提前下班-已结算.png"));clock.WindowState=FormWindowState.Minimized;
                    verify(clock.Text=="¥"+clock.DisplayedToday.ToString("F2",CultureInfo.InvariantCulture)&&clock.MoneyBar.Text==clock.Text,"任务栏立即同步结算金额");
                    clock.WindowState=FormWindowState.Normal;settle.PerformClick();verify(!Payroll.IsSettled(ui,Payroll.ChinaNow())&&settle.Text=="提前下班","撤销按钮恢复计时状态");
                    Art.Snapshot(clock,Path.Combine(folder,"提前下班-主界面.png"));
                    using(SettingsForm settings=new SettingsForm(ui,true)){settings.Show();Application.DoEvents();Button details=settings.Controls.OfType<Button>().Single(b=>b.Text=="工资明细 / 导出");verify(details.Visible&&settings.ClientRectangle.Contains(details.Bounds),"明细入口移入设置且完整可见");Art.Snapshot(settings,Path.Combine(folder,"提前下班-设置入口.png"));settings.Close();}
                    clock.Close();
                }
            }finally{Store.PathName=old;}
            File.WriteAllLines(Path.Combine(folder,"提前下班验证结果.txt"),new[]{"全部通过："+checks.Count+"项提前结算与界面验证"}.Concat(checks),new UTF8Encoding(false));
        }
        public static void Run(){
            Config c=Reference();c.LunchEnabled=true;Payroll.Validate(c);
            Check(!Payroll.Scheduled(c,At(17,0))&&Payroll.Scheduled(c,At(10,0))&&!Payroll.Scheduled(c,At(11,0)),"单双休锚点与跨周计算");
            Check(Payroll.CalendarDays(c,At(1,0))==24,"10月日历应出勤24天");
            Check(Near(Pay(c,12,10).MonthOvertime,210m)&&Near(Pay(c,12,10).MonthMeal,40m),"预置10小时模拟加班与四次餐补合计250元");
            Check(Near(Pay(c,12,10).MonthBase,6000m/21.75m*3m),"入职前无工资，8/9/10日累计三个工作日");
            Check(Near(Pay(c,12,10).Today.Total,0m),"10:00今日工资从零开始");
            Check(Near(Pay(c,12,10,0,1).Today.Base,6000m/21.75m/27000m),"普通工作每秒金额正确");
            Check(Near(Pay(c,12,12).Today.Total,Pay(c,12,13,29,59).Today.Total),"午休不计薪");
            Check(Pay(c,12,13,30,1).Today.Base>Pay(c,12,13,30).Today.Base,"13:30恢复计薪");
            Check(Near(Pay(c,12,19).Today.Base,6000m/21.75m),"19:00普通工资封顶到日薪");
            Check(Near(Pay(c,7,19).Month,0m),"入职前整月累计为零");
            Check(!Payroll.ShouldPrompt(c,At(12,18,59,59))&&Payroll.ShouldPrompt(c,At(12,19)),"工作日19:00到点触发询问");
            Check(!Payroll.ShouldPrompt(c,At(11,19)),"休息日不自动询问");
            Config yes=Store.Clone(c);Payroll.Decide(yes,At(12,19),true);
            Check(!Payroll.ShouldPrompt(yes,At(12,19,1)),"已回答后当天不重复询问");
            Check(Near(Pay(yes,12,19,59,59).Today.Overtime,0)&&Near(Pay(yes,12,19,59,59).Today.Meal,0),"确认加班后19至20点不提前计加班或餐补");
            Check(Near(Pay(yes,12,20).Today.Overtime,0)&&Near(Pay(yes,12,20).Today.Meal,10m),"20:00开始时餐补到账一次");
            Check(Near(Pay(yes,12,20,0,1).Today.Overtime,21m/3600m),"20:00之后按每秒加班费计算");
            Check(Near(Pay(yes,12,21).Today.Overtime,21m)&&Near(Pay(yes,12,21).Today.Meal,10m),"1小时加班21元，餐补不重复");
            Payroll.Stop(yes,At(12,21,30));Check(Near(Pay(yes,12,23).Today.Overtime,31.5m),"点击结束后加班金额停止增长");
            Payroll.Decide(yes,At(12,22),true);Payroll.Stop(yes,At(12,22,30));
            Check(Near(Pay(yes,12,23).Today.Overtime,42m)&&Near(Pay(yes,12,23).Today.Meal,10m),"同日重开计时不重叠，餐补仍只计一次");
            Config no=Store.Clone(c);Payroll.Decide(no,At(12,19),false);
            Check(Near(Pay(no,12,23).Today.Overtime,0)&&Near(Pay(no,12,23).Today.Meal,0),"回答否不计加班与餐补");
            Config pending=Store.Clone(c);pending.Decisions["2020-10-12"]="pending";
            Check(!Payroll.ShouldPrompt(pending,At(12,20))&&Near(Pay(pending,12,23).Today.Overtime,0),"关闭/未答复询问不计钱且不反复弹窗");
            Config overnight=Store.Clone(c);Payroll.Decide(overnight,At(12,19),true);
            Check(Near(Pay(overnight,13,9).MonthOvertime-210m,84m)&&Near(Pay(overnight,13,9).Today.Overtime,0),"跨午夜最多计到当日24:00，不自动计入第二天");
            Config canceled=Store.Clone(c);Payroll.Decide(canceled,At(12,19),true);Payroll.Stop(canceled,At(12,19,30));
            Check(Near(Pay(canceled,12,23).Today.Overtime,0)&&Near(Pay(canceled,12,23).Today.Meal,0),"20:00前取消计划不计餐补");
            Config late=Store.Clone(c);Payroll.Decide(late,At(12,21),true);
            Check(Near(Pay(late,12,21).Today.Overtime,21m),"晚开程序确认是时按提示从20:00追计");
            c.Mode="calendar";Check(Near(Pay(c,12,19).Today.Base,250m)&&Near(Pay(c,12,19).Month,1250m),"24天口径重算今日与历史");
            c.CompanyDays["2020-10"]=22m;Check(Near(Pay(c,12,19).Today.Base,6000m/22m),"公司指定应出勤天数生效");
            Check(Payroll.Divisor(c,At(1,0).AddMonths(1))!=22m,"手填天数仅作用于指定月份");
            c.CompanyDays.Clear();c.Calendar.Add(new DayRule{Date="2020-10-12",Kind="leave"});
            Check(Payroll.CalendarDays(c,At(1,0))==24&&Near(Pay(c,12,19).Today.Base,0)&&!Payroll.ShouldPrompt(c,At(12,19)),"请假不减应出勤分母，不计当天底薪且不询问加班");
            c.Calendar.Clear();c.Calendar.Add(new DayRule{Date="2020-10-12",Kind="rest"});
            Check(Payroll.CalendarDays(c,At(1,0))==23&&Near(Pay(c,12,19).Today.Base,0),"节假日/调休休息改变自动应出勤天数");
            c.Calendar.Add(new DayRule{Date="2020-10-11",Kind="work"});Check(Payroll.Scheduled(c,At(11,0)),"调休上班覆盖默认周日休息");
            c=Reference();c.LunchEnabled=true;c.WorkStart="09:00";c.LunchEnd="13:00";Check(Near(Payroll.DailySeconds(c),32400m),"可编辑工作与午休参数，自动重算9小时工日");
            c=Reference();c.LunchEnabled=false;Check(Near(Payroll.DailySeconds(c),32400m),"关闭午休扣时生效");
            Check(Pay(c,12,13).Today.Base>Pay(c,12,12).Today.Base,"午休期间底薪连续增长");
            Check(Near(Pay(c,12,19).Today.Base,6000m/21.75m),"午休计薪仍保持每日总额不变");
            Check(Near(Pay(c,12,12,0,1).Today.Base-Pay(c,12,12).Today.Base,6000m/21.75m/32400m),"午休期间每秒按9小时工日累计");
            c=Reference();c.Mode="calendar";c.Schedule="double";Check(Payroll.CalendarDays(c,At(1,0))==22,"固定双休模式支持");
            c.Schedule="single";Check(Payroll.CalendarDays(c,At(1,0))==27,"固定单休模式支持");
            c=Reference();c.Overtime.Add(new Overtime{Date="2020-10-12",Hours=1.25m});
            Check(Near(Pay(c,12,19).Today.Overtime,26.25m)&&Near(Pay(c,12,19).Today.Meal,10m),"当日手动补录已完成时长可计入今日与月累计");
            c.Overtime.Add(new Overtime{Date="2020-10-12",Hours=.5m});Check(Near(Pay(c,12,19).Today.Meal,10m),"同日多笔手动记录仍只有一份餐补");
            c=Reference();c.HireDate="2020-10-01";Check(Pay(c,31,23).MonthBase<=6000m,"月底薪累计上限6000元");
            c.CapBase=false;Check(Pay(c,31,23).MonthBase>6000m,"底薪月封顶可关闭");
            Check(Near(Payroll.Calculate(c,new DateTime(2020,11,1)).Month,0),"新月份不会重复旧月份加班");
            bool rejected=false;c.WorkEnd="09:00";try{Payroll.Validate(c);}catch{rejected=true;}Check(rejected,"非法上下班时段被拒绝");
            c=Reference();c.Sessions.Add(new Session{Date="2020-10-12",Start="2020-10-12T20:00:00",End="2020-10-12T21:00:00"});c.Sessions.Add(new Session{Date="2020-10-12",Start="2020-10-12T20:30:00",End="2020-10-12T21:30:00"});
            rejected=false;try{Payroll.Validate(c);}catch{rejected=true;}Check(rejected,"重叠的加班计时记录被拒绝");
            string csv=Reports.Csv(Pay(Reference(),12,19));Check(csv.Contains("2020-10-08")&&csv.Contains("餐补")&&csv.Split('\n').Length==15,"月度CSV覆盖日期、分项、合计");
            Check(Math.Abs((Payroll.ChinaNow()-DateTime.UtcNow).TotalHours-8)<.001,"固定东八区时钟");
            string data=Path.Combine(Store.Folder,"测试数据.json"),old=Store.PathName;
            try{Store.PathName=data;c=Reference();Payroll.Decide(c,At(12,19),true);Store.Save(c);Config restored=Store.Load();Check(restored.Decisions["2020-10-12"]=="yes"&&Near(Pay(restored,12,21).Today.Overtime,21m),"已答复与加班状态跨重启恢复，不重复计餐补");}finally{Store.PathName=old;if(File.Exists(data))File.Delete(data);}
            c=Reference();c.AttendanceEnabled=true;c.Mode="calendar";
            Check(c.MonthlySalary==6000m&&c.AttendanceBonus==600m,"薪资拆分为6000底薪及600全勤");
            c.AttendanceStatus["2020-10"]="pending";
            Check(Near(Pay(c,12,19).Today.Base,6000m/24m)&&Near(Pay(c,12,19).MonthAttendance,0),"资格待确认时仅计6000底薪，不提前计全勤");
            c.AttendanceStatus["2020-10"]="yes";c.AttendanceProrateHire=false;
            Check(Near(Pay(c,30,23).MonthAttendance,0),"月底计入模式不会提前计奖励");
            Check(Near(Pay(c,31,19).MonthAttendance,600m)&&Near(Pay(c,31,19).Today.Attendance,600m),"有全勤资格时月底计600元且与明细一致");
            c.AttendanceProrateHire=true;Check(Near(Pay(c,31,19).MonthAttendance,600m*19m/24m),"入职首月全勤按19个应出勤日折算");
            c.AttendanceMode="daily";
            Check(Near(Pay(c,12,19).Today.Attendance,600m/24m),"逐日全勤独立计入今日金额");
            Check(Near(Pay(c,12,12,0,1).Today.Attendance-Pay(c,12,12).Today.Attendance,600m/24m/32400m),"午休期间全勤逐秒累计");
            c.AttendanceStatus["2020-10"]="auto";c.Calendar.Add(new DayRule{Date="2020-10-12",Kind="leave"});
            Check(Near(Pay(c,12,19).MonthAttendance,0),"自动资格下请假取消整月全勤");
            c.AttendanceCancelLeave=false;Check(Pay(c,12,19).MonthAttendance>0,"请假是否取消全勤可关闭");
            c.Calendar.Clear();c.AttendanceIssues.Add(new AttendanceIssue{Date="2020-10-09",Kind="late",Note="测试迟到"});
            Check(Near(Pay(c,12,19).MonthAttendance,0),"迟到异常取消自动全勤资格");
            c.AttendanceCancelLate=false;Check(Pay(c,12,19).MonthAttendance>0,"迟到取消全勤规则可关闭");c.AttendanceCancelLate=true;
            c.AttendanceStatus["2020-10"]="yes";Check(Pay(c,12,19).MonthAttendance>0,"公司确认有全勤可覆盖录入异常");
            c.AttendanceStatus["2020-10"]="no";Check(Near(Pay(c,12,19).MonthAttendance,0),"无全勤资格不计奖");
            c.AttendanceStatus["2020-10"]="yes";c.AttendanceProrateHire=false;
            Check(Near(Pay(c,31,19).MonthAttendance,600m),"首月不折算时逐日累计最终为600元");
            Check(Reports.Csv(Pay(c,12,19)).Contains("全勤奖"),"CSV包含独立全勤列");
            Check(Near(Payroll.Calculate(c,new DateTime(2020,11,1)).MonthAttendance,0),"全勤资格逐月保存，不重复计入新月份");
            try{Store.PathName=data;Store.Save(c);Config restored=Store.Load();Check(restored.AttendanceBonus==600m&&restored.AttendanceStatus["2020-10"]=="yes"&&restored.AttendanceIssues.Count==1,"全勤金额、资格和异常记录重启后保留");}finally{Store.PathName=old;if(File.Exists(data))File.Delete(data);}
            File.WriteAllLines(Path.Combine(Store.Folder,"验证结果.txt"),new[]{"全部通过："+passed.Count+" 项计薪与状态验证"}.Concat(passed),new UTF8Encoding(false));
        }
        public static void UI(string folder){
            string old=Store.PathName;Directory.CreateDirectory(folder);Store.PathName=Path.Combine(folder,"界面测试配置.json");
            try{
                Config c=Reference();c.AskOvertime=false;Store.Save(c);
                string background=Path.Combine(folder,"测试背景.png");
                using(Bitmap sample=new Bitmap(24,24))using(Graphics drawing=Graphics.FromImage(sample)){drawing.Clear(Color.LightBlue);sample.Save(background,System.Drawing.Imaging.ImageFormat.Png);}
                using(ClockForm clock=new ClockForm(c,true)){
                    clock.Show();Application.DoEvents();if(clock.Width!=230||clock.Height!=193)throw new Exception("主界面尺寸不是230×193");
                    foreach(Button button in clock.Controls.OfType<Button>().Where(b=>b.Top>140)){
                        if(button.Height<button.Font.Height+6)throw new Exception("底部按钮高度不足以显示文字");
                        foreach(Point corner in new[]{new Point(button.Left,button.Top),new Point(button.Right-1,button.Top),new Point(button.Left,button.Bottom-1),new Point(button.Right-1,button.Bottom-1)})if(!clock.Region.IsVisible(corner))throw new Exception("底部按钮被窗口圆角裁切");
                    }
                    DateTime first=clock.Current.Now;DateTime until=DateTime.UtcNow.AddSeconds(2.2);
                    while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}
                    if(clock.Current.Now<=first.AddSeconds(1))throw new Exception("1秒刷新计时器未更新");
                    clock.WindowState=FormWindowState.Minimized;clock.Tick();
                    if(clock.Text!="¥"+clock.Current.Today.Total.ToString("F2",CultureInfo.InvariantCulture))throw new Exception("最小化任务栏标题包含多余文本");
                    if(!clock.MoneyBar.Visible||clock.MoneyBar.Text!=clock.Text||!Screen.PrimaryScreen.Bounds.Contains(clock.MoneyBar.Bounds))throw new Exception("任务栏右侧金额条显示失败");
                    DateTime minimizedAt=clock.Current.Now;until=DateTime.UtcNow.AddSeconds(2.2);
                    while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(20);}
                    if(clock.Current.Now<=minimizedAt.AddSeconds(1)||!clock.ShowInTaskbar||clock.Text!="¥"+clock.Current.Today.Total.ToString("F2",CultureInfo.InvariantCulture)||clock.MoneyBar.Text!=clock.Text)throw new Exception("最小化后实时金额更新失败");
                    clock.WindowState=FormWindowState.Normal;clock.Tick();
                    if(clock.MoneyBar.Visible)throw new Exception("恢复桌宠后金额条没有隐藏");
                    using(SettingsForm settings=new SettingsForm(c,true)){
                        settings.Show();Application.DoEvents();settings.VerifyEditing(background);settings.SaveAll();if(settings.Saved==null)throw new Exception("二级设置没有保存");
                    }
                    Config saved=Store.Load();if(saved.Mode!="calendar"||saved.CompanyDays[Payroll.ChinaNow().ToString("yyyy-MM")]!=22m)throw new Exception("设置保存后重开丢失");
                    if(saved.AttendanceBonus!=600m||saved.AttendanceMode!="daily"||!saved.AttendanceProrateHire||saved.AttendanceStatus[Payroll.ChinaNow().ToString("yyyy-MM")]!="auto")throw new Exception("全勤设置保存后丢失");
                    if(Path.IsPathRooted(saved.BackgroundImage)||!File.Exists(Path.Combine(Store.Folder,saved.BackgroundImage))||saved.BackgroundFit!="contain"||saved.PanelOpacity!=40)throw new Exception("自定义背景复制或保存失败");
                    File.Delete(background);
                    using(ClockForm withBackground=new ClockForm(saved,true)){withBackground.Show();Application.DoEvents();Art.Snapshot(withBackground,Path.Combine(folder,"背景验证预览.png"));withBackground.Close();}
                    clock.Close();
                }
                using(Icon icon=Art.LoadIcon()){if(icon.Width<16)throw new Exception("猫猫图标不可用");}
                File.WriteAllLines(Path.Combine(folder,"界面验证结果.txt"),new[]{"全部通过：12项界面与运行验证","主界面230×193像素，宽高均为第一版一半","底部按钮文字高度充足，四角均未被窗体裁切","窗口实时刷新计时器有效","最小化任务栏标题仅含今日金额","最小化后任务栏金额仍实时更新","右侧金额条显示且与今日金额同步更新","恢复主界面后右侧金额条隐藏","固定/日历/公司天数实时预览","二级设置保存并可重新加载","全勤金额、计入方式、首月折算及月资格保存可恢复","背景图片复制、相对路径保存与重新渲染正常","电子猫图标有效"},new UTF8Encoding(false));
            }finally{Store.PathName=old;}
        }
        public static void RestoreUI(string folder){
            string old=Store.PathName;Store.PathName=Path.Combine(folder,"恢复测试配置.json");
            Config c=Reference();c.AskOvertime=false;Exception failure=null;int stage=0;
            List<string> checks=new List<string>();DateTime deadline=DateTime.UtcNow.AddSeconds(20);
            using(System.Windows.Forms.Timer probe=new System.Windows.Forms.Timer{Interval=100}){
                try{
                    Program.RunInstance(c,delegate(ClockForm clock){
                        clock.Shown+=delegate{probe.Start();};
                        probe.Tick+=delegate{
                            try{
                                if(DateTime.UtcNow>deadline)throw new Exception("窗口恢复测试超时，阶段 "+stage);
                                if(stage==0){
                                    clock.Close();
                                    if(clock.Visible||clock.ShowInTaskbar)throw new Exception("关闭后未收到托盘");
                                    checks.Add("点击关闭后桌宠隐藏到托盘");
                                    using(var child=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath){UseShellExecute=false,CreateNoWindow=true})){}
                                    stage=1;
                                }else if(stage==1&&clock.Visible&&clock.ShowInTaskbar&&clock.WindowState==FormWindowState.Normal){
                                    checks.Add("再次启动同一EXE恢复隐藏桌宠，未产生第二个窗口");
                                    clock.WindowState=FormWindowState.Minimized;
                                    if(clock.Text!="¥"+clock.Current.Today.Total.ToString("F2",CultureInfo.InvariantCulture))throw new Exception("最小化时标题未立即变为今日金额");
                                    if(!clock.MoneyBar.Visible)throw new Exception("最小化时右侧金额条未显示");
                                    stage=2;deadline=DateTime.UtcNow.AddSeconds(20);
                                }else if(stage==2){
                                    checks.Add("真实运行窗体最小化立即显示金额标题并保留任务栏按钮");
                                    using(var child=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath){UseShellExecute=false,CreateNoWindow=true})){}
                                    stage=3;
                                }else if(stage==3&&clock.Visible&&clock.ShowInTaskbar&&clock.WindowState==FormWindowState.Normal){
                                    if(clock.MoneyBar.Visible)throw new Exception("快捷方式恢复桌宠后金额条未隐藏");
                                    checks.Add("再次启动同一EXE恢复已最小化桌宠");
                                    probe.Stop();clock.Config.CloseToTray=false;clock.Close();stage=4;
                                }
                            }catch(Exception ex){failure=ex;probe.Stop();clock.Config.CloseToTray=false;clock.Close();}
                        };
                    });
                    if(failure!=null)throw failure;
                    if(stage!=4)throw new Exception("恢复测试未完成");
                    File.WriteAllLines(Path.Combine(folder,"恢复验证结果.txt"),new[]{"全部通过：4项真实跨进程启动与恢复验证"}.Concat(checks),new UTF8Encoding(false));
                }finally{Store.PathName=old;}
            }
        }
    }
}



