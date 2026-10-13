using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace SalaryPet {
    public class Overtime { public string Date {get;set;} public decimal Hours {get;set;} }
    public class DayRule { public string Date {get;set;} public string Kind {get;set;} public string Note {get;set;} }
    public class Session { public string Date {get;set;} public string Start {get;set;} public string End {get;set;} }
    public class AttendanceIssue { public string Date {get;set;} public string Kind {get;set;} public string Note {get;set;} }
    public class Config {
        public int Version {get;set;}
        public decimal MonthlySalary {get;set;} public string HireDate {get;set;}
        public string DoubleWeekMonday {get;set;} public bool AnchorIsDouble {get;set;}
        public string Schedule {get;set;} public int SingleRestDay {get;set;}
        public int DoubleRestDay1 {get;set;} public int DoubleRestDay2 {get;set;}
        public string Mode {get;set;} public decimal FixedDays {get;set;} public bool CapBase {get;set;}
        public string WorkStart {get;set;} public string WorkEnd {get;set;}
        public bool LunchEnabled {get;set;} public string LunchStart {get;set;} public string LunchEnd {get;set;}
        public string PromptTime {get;set;} public string OvertimeStart {get;set;}
        public decimal OvertimeRate {get;set;} public decimal MealAllowance {get;set;}
        public bool AskOvertime {get;set;} public bool Startup {get;set;} public bool TopMost {get;set;}
        public bool CloseToTray {get;set;}
        public bool TaskbarMoneyBar {get;set;}
        public bool AttendanceEnabled {get;set;} public decimal AttendanceBonus {get;set;}
        public string AttendanceMode {get;set;} public bool AttendanceProrateHire {get;set;}
        public bool AttendanceCancelLeave {get;set;} public bool AttendanceCancelLate {get;set;} public Dictionary<string,string> AttendanceStatus {get;set;}
        public List<AttendanceIssue> AttendanceIssues {get;set;}
        public string BackgroundColor {get;set;} public string BackgroundImage {get;set;}
        public string BackgroundFit {get;set;} public int PanelOpacity {get;set;}
        public int Decimals {get;set;} public int Left {get;set;} public int Top {get;set;}
        public Dictionary<string,decimal> CompanyDays {get;set;}
        public List<Overtime> Overtime {get;set;} public List<DayRule> Calendar {get;set;}
        public List<Session> Sessions {get;set;} public Dictionary<string,string> Decisions {get;set;}
        public Dictionary<string,string> EarlyClosures {get;set;}
        public Dictionary<string,string> EarlyStarts {get;set;}
        public Config() {
            DateTime today=Payroll.ChinaNow().Date;Version=3;MonthlySalary=6000m;HireDate=new DateTime(today.Year,today.Month,1).ToString("yyyy-MM-dd");DoubleWeekMonday=today.AddDays(-(((int)today.DayOfWeek+6)%7)).ToString("yyyy-MM-dd");AnchorIsDouble=true;
            Schedule="alternate";SingleRestDay=0;DoubleRestDay1=6;DoubleRestDay2=0;
            Mode="fixed";FixedDays=21.75m;CapBase=true;
            WorkStart="10:00";WorkEnd="19:00";LunchEnabled=false;LunchStart="12:00";LunchEnd="13:30";
            PromptTime="19:00";OvertimeStart="20:00";OvertimeRate=20m;MealAllowance=15m;AskOvertime=true;
            Startup=false;TopMost=true;CloseToTray=true;TaskbarMoneyBar=true;
            AttendanceEnabled=true;AttendanceBonus=600m;AttendanceMode="monthly";AttendanceProrateHire=true;AttendanceCancelLeave=true;AttendanceCancelLate=true;
            AttendanceStatus=new Dictionary<string,string>();AttendanceIssues=new List<AttendanceIssue>();
            BackgroundColor="#FFFFFF";BackgroundImage="";BackgroundFit="cover";PanelOpacity=94;Decimals=2;Left=-1;Top=-1;
            CompanyDays=new Dictionary<string,decimal>(); Calendar=new List<DayRule>();Sessions=new List<Session>();Decisions=new Dictionary<string,string>();
            EarlyClosures=new Dictionary<string,string>();
            EarlyStarts=new Dictionary<string,string>();
            Overtime=new List<Overtime>();
        }
    }
    public class DayPay {
        public DateTime Date;public string Type;public decimal WorkHours,Base,Attendance,OvertimeHours,Overtime,Meal;
        public decimal Total {get{return Base+Attendance+Overtime+Meal;}}
    }
    public class Result {
        public DateTime Now;public decimal Divisor,DayRate,HourRate,MonthBase,MonthAttendance,AttendancePotential,MonthOvertime,MonthMeal;
        public string AttendanceState;
        public DayPay Today;public List<DayPay> Days;public string State;public bool ActiveOvertime;
        public decimal Month {get{return MonthBase+MonthAttendance+MonthOvertime+MonthMeal;}}
    }
    public static class Payroll {
        public static DateTime ChinaNow(){return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).DateTime;}
        public static DateTime Date(string s){return DateTime.ParseExact(s,"yyyy-MM-dd",CultureInfo.InvariantCulture);}
        public static TimeSpan Time(string s){return TimeSpan.ParseExact(s,@"hh\:mm",CultureInfo.InvariantCulture);}
        public static DateTime Stamp(string s){return DateTime.ParseExact(s,"yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture);}
        public static string Stamp(DateTime d){return d.ToString("yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture);}
        public static bool DoubleWeek(Config c,DateTime d){
            DateTime monday=d.Date.AddDays(-(((int)d.DayOfWeek+6)%7));
            int weeks=(int)((monday-Date(c.DoubleWeekMonday)).TotalDays/7);
            bool same=((weeks%2)+2)%2==0;return c.AnchorIsDouble?same:!same;
        }
        public static DayRule Override(Config c,DateTime d){return c.Calendar.LastOrDefault(x=>Date(x.Date)==d.Date);}
        public static bool Scheduled(Config c,DateTime d){
            DayRule rule=Override(c,d);if(rule!=null){if(rule.Kind=="work"||rule.Kind=="leave")return true;if(rule.Kind=="rest")return false;}
            bool dbl=c.Schedule=="double"||(c.Schedule=="alternate"&&DoubleWeek(c,d));
            int wd=(int)d.DayOfWeek;
            return dbl?(wd!=c.DoubleRestDay1&&wd!=c.DoubleRestDay2):wd!=c.SingleRestDay;
        }
        public static string DayType(Config c,DateTime d){
            DayRule rule=Override(c,d);
            if(rule!=null)return rule.Kind=="leave"?"请假":rule.Kind=="rest"?"休息/假期":"调休上班";
            return Scheduled(c,d)?"工作日":"休息日";
        }
        public static int CalendarDays(Config c,DateTime m){int n=0;for(int i=1;i<=DateTime.DaysInMonth(m.Year,m.Month);i++)if(Scheduled(c,new DateTime(m.Year,m.Month,i)))n++;return n;}
        public static decimal Divisor(Config c,DateTime m){
            if(c.Mode=="fixed")return c.FixedDays;decimal n;
            if(c.CompanyDays.TryGetValue(m.ToString("yyyy-MM"),out n))return n;
            int count=CalendarDays(c,m);return count>0?count:1;
        }
        public static decimal DailySeconds(Config c){
            decimal total=(decimal)(Time(c.WorkEnd)-Time(c.WorkStart)).TotalSeconds;
            if(c.LunchEnabled)total-=(decimal)(Time(c.LunchEnd)-Time(c.LunchStart)).TotalSeconds;return total;
        }
        static decimal Segment(DateTime day,DateTime now,TimeSpan start,TimeSpan end){
            DateTime a=day+start,b=day+end;if(now<=a||end<=start)return 0;
            return (decimal)((now<b?now:b)-a).Ticks/TimeSpan.TicksPerSecond;
        }
        public static decimal Worked(Config c,DateTime d,DateTime now){
            DayRule rule=Override(c,d);
            if(d<Date(c.HireDate)||!Scheduled(c,d)||(rule!=null&&rule.Kind=="leave"))return 0;
            string settledAt;
            if(c.EarlyClosures.TryGetValue(d.ToString("yyyy-MM-dd"),out settledAt)&&now>=Stamp(settledAt))return DailySeconds(c);
            string earlyAt;decimal early=0;
            if(c.EarlyStarts.TryGetValue(d.ToString("yyyy-MM-dd"),out earlyAt))early=Segment(d,now,Stamp(earlyAt).TimeOfDay,Time(c.WorkStart));
            if(!c.LunchEnabled)return Math.Min(DailySeconds(c),early+Segment(d,now,Time(c.WorkStart),Time(c.WorkEnd)));
            return Math.Min(DailySeconds(c),early+Segment(d,now,Time(c.WorkStart),Time(c.LunchStart))+Segment(d,now,Time(c.LunchEnd),Time(c.WorkEnd)));
        }
        public static bool IsEarlyStarted(Config c,DateTime now){string at;return c.EarlyStarts.TryGetValue(now.ToString("yyyy-MM-dd"),out at)&&now>=Stamp(at);}
        public static TimeSpan ShiftedTime(Config c,DateTime now,string normal){
            TimeSpan time=Time(normal);string at;
            if(c.EarlyStarts.TryGetValue(now.ToString("yyyy-MM-dd"),out at)&&now>=Stamp(at)){
                TimeSpan advance=Time(c.WorkStart)-Stamp(at).TimeOfDay;
                if(advance>TimeSpan.Zero)time-=advance;
            }
            return time<TimeSpan.Zero?TimeSpan.Zero:time;
        }
        public static TimeSpan WorkEndToday(Config c,DateTime now){return ShiftedTime(c,now,c.WorkEnd);}
        public static TimeSpan OvertimeStartToday(Config c,DateTime now){return ShiftedTime(c,now,c.OvertimeStart);}
        public static TimeSpan PromptToday(Config c,DateTime now){return ShiftedTime(c,now,c.PromptTime);}
        static void ShiftWaitingOvertime(Config c,DateTime now,TimeSpan oldTime,TimeSpan newTime){
            foreach(Session session in c.Sessions.Where(s=>Date(s.Date)==now.Date&&String.IsNullOrEmpty(s.End)&&Stamp(s.Start)>now&&Stamp(s.Start)==now.Date+oldTime))session.Start=Stamp(now.Date+newTime);
        }
        public static bool CanStartEarly(Config c,DateTime now){return now.Date>=Date(c.HireDate)&&Scheduled(c,now.Date)&&DayType(c,now.Date)!="请假"&&now.TimeOfDay<Time(c.WorkStart)&&!IsSettled(c,now);}
        public static void StartEarly(Config c,DateTime now){
            if(IsEarlyStarted(c,now))return;
            if(!CanStartEarly(c,now))throw new Exception("仅应出勤日的正常上班时间前可提前上班，请假日不适用。");
            TimeSpan oldTime=OvertimeStartToday(c,now);
            c.EarlyStarts[now.ToString("yyyy-MM-dd")]=Stamp(now);
            ShiftWaitingOvertime(c,now,oldTime,OvertimeStartToday(c,now));
        }
        public static void UndoEarlyStart(Config c,DateTime now){
            if(IsSettled(c,now))throw new Exception("今日已结算，请先撤销结算后再撤销提前上班。");
            TimeSpan oldTime=OvertimeStartToday(c,now);
            c.EarlyStarts.Remove(now.ToString("yyyy-MM-dd"));
            ShiftWaitingOvertime(c,now,oldTime,OvertimeStartToday(c,now));
        }
        public static Session LiveSession(Config c,DateTime now){return c.Sessions.LastOrDefault(s=>Date(s.Date)==now.Date&&String.IsNullOrEmpty(s.End));}
        public static bool IsSettled(Config c,DateTime now){string at;return c.EarlyClosures.TryGetValue(now.ToString("yyyy-MM-dd"),out at)&&now>=Stamp(at);}
        public static bool CanSettle(Config c,DateTime now){return now.Date>=Date(c.HireDate)&&Scheduled(c,now.Date)&&DayType(c,now.Date)!="请假"&&(now.TimeOfDay>=Time(c.WorkStart)||IsEarlyStarted(c,now));}
        public static void Settle(Config c,DateTime now){
            if(IsSettled(c,now))return;
            if(!CanSettle(c,now))throw new Exception("仅已上班的应出勤日可以提前结算，请假日不适用。");
            foreach(Session s in c.Sessions.Where(s=>Date(s.Date)==now.Date&&String.IsNullOrEmpty(s.End))){
                DateTime start=Stamp(s.Start),end=now<start?start:now;
                if(end>now.Date.AddDays(1))end=now.Date.AddDays(1);s.End=Stamp(end);
            }
            c.EarlyClosures[now.ToString("yyyy-MM-dd")]=Stamp(now);
        }
        public static void UndoSettlement(Config c,DateTime now){c.EarlyClosures.Remove(now.ToString("yyyy-MM-dd"));}
        public static bool ShouldPrompt(Config c,DateTime now){
            return c.AskOvertime&&!IsSettled(c,now)&&now.Date>=Date(c.HireDate)&&Scheduled(c,now.Date)&&DayType(c,now.Date)!="请假"
                &&now.TimeOfDay>=PromptToday(c,now)&&!c.Decisions.ContainsKey(now.ToString("yyyy-MM-dd"));
        }
        public static void Decide(Config c,DateTime now,bool yes){
            if(yes&&IsSettled(c,now))throw new Exception("今日已结算，请先撤销结算后再开始加班。");
            string day=now.ToString("yyyy-MM-dd");c.Decisions[day]=yes?"yes":"no";
            if(yes&&LiveSession(c,now)==null){
                DateTime start=now.Date+OvertimeStartToday(c,now);
                if(c.Sessions.Any(s=>s.Date==day)&&now>start)start=now;
                c.Sessions.Add(new Session{Date=day,Start=Stamp(start),End=""});
            }
        }
        public static void Stop(Config c,DateTime now){
            Session live=LiveSession(c,now);if(live==null)return;
            DateTime start=Stamp(live.Start),finish=now<start?start:now;
            if(finish>start.Date.AddDays(1))finish=start.Date.AddDays(1);
            live.End=Stamp(finish);
        }
        public static Result Calculate(Config c,DateTime now){
            Result r=new Result{Now=now,Divisor=Divisor(c,now),Days=new List<DayPay>()};
            r.DayRate=c.MonthlySalary/r.Divisor;r.HourRate=r.DayRate/(DailySeconds(c)/3600m);
            DateTime first=new DateTime(now.Year,now.Month,1),hire=Date(c.HireDate);decimal remaining=c.MonthlySalary;
            int count=DateTime.DaysInMonth(now.Year,now.Month);
            for(int i=1;i<=count;i++){
                DateTime d=first.AddDays(i-1);DayPay pay=new DayPay{Date=d,Type=DayType(c,d)};
                if(d<=now.Date&&d>=hire){
                    decimal seconds=Worked(c,d,now);pay.WorkHours=seconds/3600m;
                    string settledAt;if(c.EarlyClosures.TryGetValue(d.ToString("yyyy-MM-dd"),out settledAt)&&now>=Stamp(settledAt)&&seconds>0)pay.Type+=" · 提前结算";
                    string earlyAt;if(c.EarlyStarts.TryGetValue(d.ToString("yyyy-MM-dd"),out earlyAt)&&now>=Stamp(earlyAt)&&seconds>0)pay.Type+=" · 提前上班";
                    decimal raw=seconds/DailySeconds(c)*r.DayRate;
                    pay.Base=c.CapBase?Math.Min(Math.Max(0,remaining),raw):raw;remaining-=pay.Base;
                    decimal manual=c.Overtime.Where(o=>Date(o.Date)==d&&o.Hours>0).Sum(o=>o.Hours);
                    bool meal=manual>0;pay.OvertimeHours=manual;
                    foreach(Session session in c.Sessions.Where(s=>Date(s.Date)==d)){
                        DateTime start=Stamp(session.Start),end=String.IsNullOrEmpty(session.End)?d.AddDays(1):Stamp(session.End);
                        if(end>d.AddDays(1))end=d.AddDays(1);
                        DateTime until=now<end?now:end;
                        if(end>start&&until>=start){meal=true;pay.OvertimeHours+=(decimal)(until-start).Ticks/TimeSpan.TicksPerHour;}
                        if(d==now.Date&&String.IsNullOrEmpty(session.End)&&now>=start&&now<end)r.ActiveOvertime=true;
                    }
                    pay.Overtime=pay.OvertimeHours*c.OvertimeRate;pay.Meal=meal?c.MealAllowance:0;
                }
                r.MonthBase+=pay.Base;r.MonthOvertime+=pay.Overtime;r.MonthMeal+=pay.Meal;r.Days.Add(pay);
                if(d==now.Date)r.Today=pay;
            }
            ApplyAttendance(c,r);
            if(now.Date<hire)r.State="尚未入职";
            else if(IsSettled(c,now))r.State="今日已结算 · 提前下班啦";
            else if(r.ActiveOvertime)r.State="加班中 · 记得适时休息";
            else if(LiveSession(c,now)!=null&&now<Stamp(LiveSession(c,now).Start))r.State=Stamp(LiveSession(c,now).Start).ToString("HH:mm")+" 开始加班计时";
            else if(DayType(c,now.Date)=="请假")r.State="请假日 · 好好休息";
            else if(!Scheduled(c,now.Date))r.State="休息日 · 放松一下";
            else if(now.TimeOfDay<Time(c.WorkStart)&&IsEarlyStarted(c,now))r.State="提前上班 · "+WorkEndToday(c,now).ToString(@"hh\:mm")+" 下班";
            else if(now.TimeOfDay<Time(c.WorkStart))r.State="上班前 · 元气满满";
            else if(now.TimeOfDay>=WorkEndToday(c,now))r.State="下班啦 · 今天辛苦了";
            else if(c.LunchEnabled&&now.TimeOfDay>=Time(c.LunchStart)&&now.TimeOfDay<Time(c.LunchEnd))r.State="午休中 · 收益暂停";
            else if(c.CapBase&&r.MonthBase>=c.MonthlySalary)r.State="本月基础收入已累计完成";
            else r.State=IsEarlyStarted(c,now)?"工作中 · "+WorkEndToday(c,now).ToString(@"hh\:mm")+" 下班":"工作中 · 每秒都算数";
            return r;
        }
        static void ApplyAttendance(Config c,Result r){
            if(!c.AttendanceEnabled){r.AttendanceState="未启用";return;}
            DateTime first=new DateTime(r.Now.Year,r.Now.Month,1),next=first.AddMonths(1),hire=Date(c.HireDate);
            if(hire>=next){r.AttendanceState="尚未入职";return;}
            int eligible=r.Days.Count(d=>d.Date>=hire&&Scheduled(c,d.Date));
            r.AttendancePotential=c.AttendanceProrateHire&&hire>first?c.AttendanceBonus*Math.Min(1m,(decimal)eligible/r.Divisor):c.AttendanceBonus;
            if(eligible==0){r.AttendanceState="无应出勤日";r.AttendancePotential=0;return;}
            string status;
            if(!c.AttendanceStatus.TryGetValue(first.ToString("yyyy-MM"),out status))status="auto";
            bool failed=c.AttendanceCancelLeave&&c.Calendar.Any(d=>d.Kind=="leave"&&Date(d.Date)>=first&&Date(d.Date)<next&&Date(d.Date)>=hire&&Date(d.Date)<=r.Now.Date);
            failed|=c.AttendanceIssues.Any(d=>Date(d.Date)>=first&&Date(d.Date)<next&&Date(d.Date)>=hire&&Date(d.Date)<=r.Now.Date&&(d.Kind=="other"||(d.Kind=="late"&&c.AttendanceCancelLate)));
            if(status=="no"||(status=="auto"&&failed)){r.AttendanceState="无全勤资格";return;}
            if(status=="pending"){r.AttendanceState="资格待确认";return;}
            if(c.AttendanceMode=="daily"){
                decimal remaining=r.AttendancePotential;
                // Full first-month award is distributed over post-hire scheduled days;
                // prorated awards follow the selected payroll divisor.
                decimal divisor=hire>first&&!c.AttendanceProrateHire?eligible:r.Divisor;
                foreach(DayPay d in r.Days){
                    decimal amount=d.WorkHours/(DailySeconds(c)/3600m)*c.AttendanceBonus/divisor;
                    d.Attendance=Math.Min(Math.Max(0,remaining),amount);remaining-=d.Attendance;r.MonthAttendance+=d.Attendance;
                }
                r.AttendanceState="逐日累计（资格按已录考勤估算）";
            }else{
                if(r.Now>=next.AddDays(-1)+WorkEndToday(c,r.Now)){
                    DayPay last=r.Days.Last();last.Attendance=r.AttendancePotential;r.MonthAttendance=last.Attendance;r.AttendanceState="月底全勤已计入";
                }else r.AttendanceState="全勤待月底结算";
            }
        }
        public static void Validate(Config c){
            foreach(var item in c.EarlyStarts){DateTime d=Date(item.Key),at=Stamp(item.Value);if(at.Date!=d)throw new Exception("提前上班日期与时间不一致。");}
            foreach(var item in c.EarlyClosures){DateTime d=Date(item.Key),at=Stamp(item.Value);if(at.Date!=d)throw new Exception("提前结算日期与时间不一致。");}
            if(c.MonthlySalary<=0||c.MonthlySalary>10000000m)throw new Exception("底薪需大于0且不超过1000万元。");
            if(c.AttendanceBonus<0||c.AttendanceBonus>10000000m||(c.AttendanceMode!="daily"&&c.AttendanceMode!="monthly"))throw new Exception("全勤金额或计入方式无效。");
            foreach(var item in c.AttendanceStatus){Date(item.Key+"-01");if(!new[]{"pending","auto","yes","no"}.Contains(item.Value))throw new Exception("全勤资格无效。");}
            foreach(var item in c.AttendanceIssues){Date(item.Date);if(!new[]{"late","early","absent","other"}.Contains(item.Kind))throw new Exception("考勤异常类型无效。");}
            Date(c.HireDate);if(Date(c.DoubleWeekMonday).DayOfWeek!=DayOfWeek.Monday)throw new Exception("单双休锚点日期必须是周一。");
            if(c.FixedDays<=0||c.FixedDays>31)throw new Exception("固定折算天数需在0至31之间。");
            if(c.Mode!="fixed"&&c.Mode!="calendar")throw new Exception("折算方式无效。");
            if(c.Schedule!="alternate"&&c.Schedule!="single"&&c.Schedule!="double")throw new Exception("排班方式无效。");
            if(c.SingleRestDay<0||c.SingleRestDay>6||c.DoubleRestDay1<0||c.DoubleRestDay1>6||c.DoubleRestDay2<0||c.DoubleRestDay2>6||c.DoubleRestDay1==c.DoubleRestDay2)throw new Exception("休息日设置无效或重复。");
            TimeSpan start=Time(c.WorkStart),end=Time(c.WorkEnd);
            if(start>=end)throw new Exception("上班时间必须早于下班时间（不支持跨日班次）。");
            if(c.LunchEnabled&&(Time(c.LunchStart)<start||Time(c.LunchEnd)>end||Time(c.LunchStart)>=Time(c.LunchEnd)))throw new Exception("午休时间需位于上下班时间之间，且开始早于结束。");
            if(DailySeconds(c)<=0)throw new Exception("有效工作时间必须大于0。");
            if(Time(c.PromptTime)>Time(c.OvertimeStart)||Time(c.OvertimeStart)<end)throw new Exception("加班计薪起点不得早于下班时间和加班询问时间。");
            if(c.OvertimeRate<0||c.MealAllowance<0)throw new Exception("加班单价和餐补不能为负数。");
            foreach(var kv in c.CompanyDays){Date(kv.Key+"-01");if(kv.Value<=0||kv.Value>31)throw new Exception("月应出勤天数需在0至31之间。");}
            foreach(var o in c.Overtime){Date(o.Date);if(o.Hours<0||o.Hours>24)throw new Exception("单笔加班小时需在0至24之间。");}
            if(c.Overtime.GroupBy(o=>o.Date).Any(g=>g.Sum(o=>o.Hours)>24))throw new Exception("同日手动加班小时合计不能超过24小时。");
            foreach(var d in c.Calendar){Date(d.Date);if(d.Kind!="work"&&d.Kind!="rest"&&d.Kind!="leave")throw new Exception("日历类型无效。");}
            if(c.Calendar.GroupBy(d=>d.Date).Any(g=>g.Count()>1))throw new Exception("同一日期只能有一条日历设置。");
            foreach(var s in c.Sessions){DateTime d=Date(s.Date),a=Stamp(s.Start);if(a.Date!=d)throw new Exception("加班记录开始日期不符。");if(!String.IsNullOrEmpty(s.End)){DateTime b=Stamp(s.End);if(b<a||b>d.AddDays(1))throw new Exception("加班结束时间无效。");}}
            foreach(var group in c.Sessions.GroupBy(s=>s.Date)){
                DateTime previousEnd=Date(group.Key);
                foreach(Session s in group.OrderBy(s=>Stamp(s.Start))){DateTime a=Stamp(s.Start),b=String.IsNullOrEmpty(s.End)?Date(s.Date).AddDays(1):Stamp(s.End);if(a<previousEnd)throw new Exception("同一天的加班计时记录不能重叠。");previousEnd=b;}
            }
            if(c.Decimals<2||c.Decimals>4||c.PanelOpacity<20||c.PanelOpacity>100)throw new Exception("显示精度或背景遮罩参数无效。");
            System.Drawing.ColorTranslator.FromHtml(c.BackgroundColor);
        }
    }
    public static class Store {
        public static string Folder=AppDomain.CurrentDomain.BaseDirectory;
        public static string PathName=Path.Combine(Folder,"计薪设置.json");
        public static Config Clone(Config c){return new JavaScriptSerializer().Deserialize<Config>(new JavaScriptSerializer().Serialize(c));}
        public static Config Load(){
            Config c=File.Exists(PathName)?new JavaScriptSerializer().Deserialize<Config>(File.ReadAllText(PathName,Encoding.UTF8)):new Config();
            if(c==null)throw new Exception("配置文件为空。");
            if(c.EarlyStarts==null)c.EarlyStarts=new Dictionary<string,string>();
            if(c.EarlyClosures==null)c.EarlyClosures=new Dictionary<string,string>();
            if(c.CompanyDays==null)c.CompanyDays=new Dictionary<string,decimal>();if(c.Calendar==null)c.Calendar=new List<DayRule>();
            if(c.Sessions==null)c.Sessions=new List<Session>();if(c.Decisions==null)c.Decisions=new Dictionary<string,string>();if(c.Overtime==null)c.Overtime=new List<Overtime>();
            if(c.AttendanceStatus==null)c.AttendanceStatus=new Dictionary<string,string>();if(c.AttendanceIssues==null)c.AttendanceIssues=new List<AttendanceIssue>();
            Payroll.Validate(c);return c;
        }
        public static void Save(Config c){
            Payroll.Validate(c);string pending=PathName+".tmp";
            File.WriteAllText(pending,new JavaScriptSerializer().Serialize(c),new UTF8Encoding(false));
            if(File.Exists(PathName))File.Replace(pending,PathName,null);else File.Move(pending,PathName);
        }
        public static void Restore(Config target,Config before){
            foreach(var p in typeof(Config).GetProperties())p.SetValue(target,p.GetValue(before,null),null);
        }
        public static void Change(Config c,Action mutate){Config before=Clone(c);try{mutate();Save(c);}catch{Restore(c,before);throw;}}
    }
    public static class Reports {
        public static string Csv(Result result){
            StringBuilder s=new StringBuilder("日期,类型,普通工时,底薪,全勤奖,加班小时,加班费,餐补,当日合计\r\n");
            foreach(DayPay d in result.Days.Where(d=>d.Date<=result.Now.Date))
                s.AppendFormat(CultureInfo.InvariantCulture,"{0:yyyy-MM-dd},{1},{2:F4},{3:F2},{4:F2},{5:F4},{6:F2},{7:F2},{8:F2}\r\n",d.Date,d.Type,d.WorkHours,d.Base,d.Attendance,d.OvertimeHours,d.Overtime,d.Meal,d.Total);
            s.AppendFormat(CultureInfo.InvariantCulture,"合计,,,{0:F2},{1:F2},,{2:F2},{3:F2},{4:F2}\r\n",result.MonthBase,result.MonthAttendance,result.MonthOvertime,result.MonthMeal,result.Month);
            return s.ToString();
        }
    }
}

