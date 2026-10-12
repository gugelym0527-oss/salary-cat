using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SalaryPet {
    public static class StartupManager {
        static string ShortcutPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"工资桌宠-电子猫.lnk");}}
        static object ReadProperty(object obj,string name){return obj.GetType().InvokeMember(name,BindingFlags.GetProperty,null,obj,null);}
        static void SetProperty(object obj,string name,object value){obj.GetType().InvokeMember(name,BindingFlags.SetProperty,null,obj,new[]{value});}
        public static void Apply(bool enabled){
            if(!enabled&&!File.Exists(ShortcutPath))return;
            object shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));object link=null;
            try{
                link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{ShortcutPath});
                string executable=Application.ExecutablePath;
                if(File.Exists(ShortcutPath)){
                    string old=Convert.ToString(ReadProperty(link,"TargetPath"));
                    if(!String.Equals(old,executable,StringComparison.OrdinalIgnoreCase)&&Convert.ToString(ReadProperty(link,"Description"))!="电子猫工资计时器")throw new Exception("同名开机启动快捷方式指向其他程序，请先处理该快捷方式。");
                }
                if(!enabled){File.Delete(ShortcutPath);return;}
                SetProperty(link,"TargetPath",executable);SetProperty(link,"WorkingDirectory",Store.Folder);SetProperty(link,"Arguments","--autostart");
                SetProperty(link,"IconLocation",Path.Combine(Store.Folder,"电子猫.ico")+",0");SetProperty(link,"Description","电子猫工资计时器");
                link.GetType().InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
            }finally{if(link!=null)Marshal.ReleaseComObject(link);Marshal.ReleaseComObject(shell);}
        }
    }
    public static class Program {
        [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")]static extern bool AllowSetForegroundWindow(int processId);
        public static EventWaitHandle RestoreSignal;
        public static string InstanceName {
            get{using(var sha=System.Security.Cryptography.SHA256.Create())return "SalaryCat-"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Application.ExecutablePath.ToLowerInvariant()))).Replace("-","").Substring(0,16);}
        }
        static void GrantExistingForeground(){
            foreach(Process p in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)){
                try{if(p.Id!=Process.GetCurrentProcess().Id&&String.Equals(p.MainModule.FileName,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase)){AllowSetForegroundWindow(p.Id);return;}}catch{}
            }
        }
        public static void RunInstance(Config c,Action<ClockForm> setup=null){
            using(EventWaitHandle request=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\"+InstanceName+".Restore")){
                bool created;
                using(Mutex instance=new Mutex(true,"Local\\"+InstanceName,out created)){
                    if(!created){GrantExistingForeground();request.Set();return;}
                    RestoreSignal=request;
                    try{
                        Store.Save(c);
                        using(ClockForm clock=new ClockForm(c)){if(setup!=null)setup(clock);Application.Run(clock);}
                    }finally{RestoreSignal=null;}
                }
            }
        }
        [STAThread]public static void Main(string[] args){
            try{
                SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length==2&&args[0]=="--make-icon"){Art.WriteIcon(args[1]);return;}
                if(args.Length>0&&args[0]=="--self-test"){SelfTests.Run();return;}
                if(args.Length==2&&args[0]=="--cat-test"){SelfTests.CatUI(args[1]);return;}
                if(args.Length==2&&args[0]=="--ui-test"){SelfTests.UI(args[1]);return;}
                if(args.Length==2&&args[0]=="--restore-test"){SelfTests.RestoreUI(args[1]);return;}
                Config c=Store.Load();
                if(args.Length==2&&args[0]=="--render"){
                    using(ClockForm clock=new ClockForm(c,true)){
                        clock.Show();Application.DoEvents();Art.Snapshot(clock,Path.Combine(args[1],"工资桌宠-预览.png"));
                        clock.WindowState=FormWindowState.Minimized;clock.Tick();Application.DoEvents();Art.Snapshot(clock.MoneyBar,Path.Combine(args[1],"任务栏金额条-预览.png"));clock.WindowState=FormWindowState.Normal;clock.Close();
                    }
                    using(SettingsForm settings=new SettingsForm(c,true)){settings.Show();Application.DoEvents();for(int n=0;n<7;n++){settings.SelectTab(n);Application.DoEvents();Art.Snapshot(settings,Path.Combine(args[1],"设置-"+new[]{"计薪","作息","加班","日历","外观","系统","全勤"}[n]+".png"));}settings.Close();}
                    using(DetailForm detail=new DetailForm(c)){detail.Show();Application.DoEvents();Art.Snapshot(detail,Path.Combine(args[1],"工资明细-预览.png"));detail.Close();}
                    using(OvertimeAskForm ask=new OvertimeAskForm(c)){ask.Show();Application.DoEvents();Art.Snapshot(ask,Path.Combine(args[1],"加班询问-预览.png"));ask.Close();}
                    return;
                }
                RunInstance(c);
            }catch(Exception ex){
                try{File.WriteAllText(Path.Combine(Store.Folder,"运行错误.txt"),ex.ToString(),Encoding.UTF8);}catch{}
                if(args.Length==0)MessageBox.Show("启动失败："+ex.Message,"工资桌宠",MessageBoxButtons.OK,MessageBoxIcon.Error);
                Environment.ExitCode=1;
            }
        }
    }
}
