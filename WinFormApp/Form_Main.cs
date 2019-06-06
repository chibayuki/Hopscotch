/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
Copyright © 2019 chibayuki@foxmail.com

跳方格 (Hopscotch)
Version 7.1.17000.1865.R8.190525-1400

This file is part of "跳方格" (Hopscotch)

"跳方格" (Hopscotch) is released under the GPLv3 license
* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace WinFormApp
{
    public partial class Form_Main : Form
    {
        #region 版本信息

        private static readonly string ApplicationName = Application.ProductName; // 程序名。
        private static readonly string ApplicationEdition = "7.1.8"; // 程序版本。

        private static readonly Int32 MajorVersion = new Version(Application.ProductVersion).Major; // 主版本。
        private static readonly Int32 MinorVersion = new Version(Application.ProductVersion).Minor; // 副版本。
        private static readonly Int32 BuildNumber = new Version(Application.ProductVersion).Build; // 版本号。
        private static readonly Int32 BuildRevision = new Version(Application.ProductVersion).Revision; // 修订版本。
        private static readonly string LabString = "R8"; // 分支名。
        private static readonly string BuildTime = "190525-1400"; // 编译时间。

        //

        private static readonly string RootDir_Product = Environment.SystemDirectory.Substring(0, 1) + @":\ProgramData\AppConfig\hopscotch"; // 根目录：此产品。
        private static readonly string RootDir_CurrentVersion = RootDir_Product + "\\" + BuildNumber + "." + BuildRevision; // 根目录：当前版本。

        private static readonly string ConfigFileDir = RootDir_CurrentVersion + @"\Config"; // 配置文件所在目录。
        private static readonly string ConfigFilePath = ConfigFileDir + @"\settings.cfg"; // 配置文件路径。

        private static readonly string LogFileDir = RootDir_CurrentVersion + @"\Log"; // 存档文件所在目录。
        private static readonly string DataFilePath = LogFileDir + @"\userdata.cfg"; // 用户数据文件路径（包含最佳成绩与游戏时长）。
        private static readonly string RecordFilePath = LogFileDir + @"\lastgame.cfg"; // 上次游戏文件路径（包含最后一次游戏记录）。

        //

        private static readonly List<Version> OldVersionList = new List<Version> // 兼容的版本列表，用于从最新的兼容版本迁移配置设置。
        {
            new Version(7, 1, 17000, 0),
            new Version(7, 1, 17000, 310),
            new Version(7, 1, 17000, 456),
            new Version(7, 1, 17000, 602),
            new Version(7, 1, 17000, 790),
            new Version(7, 1, 17000, 1760),
            new Version(7, 1, 17000, 1824)
        };

        //

        private static readonly string URL_GitHub_Base = @"https://github.com/chibayuki/Hopscotch"; // 此项目在 GitHub 的 URL。
        private static readonly string URL_GitHub_Release = URL_GitHub_Base + @"/releases/latest"; // 此项目的最新发布版本在 GitHub 的 URL。

        #endregion

        #region 配置设置变量

        private const Int32 PressSensitivity_MIN = 50; // 按压灵敏度的最小值。
        private const Int32 PressSensitivity_MAX = 200; // 按压灵敏度的最大值。
        private Int32 PressSensitivity = 100; // 按压灵敏度。

        //

        private const Int32 PlatformOpacity_MIN = 25; // 跳台不透明度的最小值。
        private const Int32 PlatformOpacity_MAX = 100; // 跳台不透明度的最大值。
        private Int32 PlatformOpacity = 75; // 跳台不透明度。

        //

        private const Com.WinForm.Theme Theme_DEFAULT = Com.WinForm.Theme.Colorful; // 主题的默认值。

        private bool UseRandomThemeColor = true; // 是否使用随机的主题颜色。

        private static readonly Color ThemeColor_DEFAULT = Color.Gray; // 主题颜色的默认值。

        private const bool ShowFormTitleColor_DEFAULT = true; // 是否显示窗体标题栏的颜色的默认值。

        private const double Opacity_MIN = 0.05; // 总体不透明度的最小值。
        private const double Opacity_MAX = 1.0; // 总体不透明度的最大值。

        //

        private bool AntiAlias = true; // 是否使用抗锯齿模式绘图。

        #endregion

        #region 游戏变量

        private static readonly Size FormClientInitialSize = new Size(585, 420); // 窗体工作区初始大小。

        //

        private Color GameUIBackColor_DEC => Me.RecommendColors.Background_DEC.ToColor(); // 游戏 UI 背景颜色（浅色）。
        private Color GameUIBackColor_INC => Me.RecommendColors.Background_INC.ToColor(); // 游戏 UI 背景颜色（深色）。

        //

        private bool GameIsOver = false; // 游戏是否已经失败。

        //

        private const double PlatformHeight = 64; // 跳台高度。
        private const double PlatformMinWidth = 14; // 跳台最小宽度。
        private const double PlatformMaxWidth = 192; // 跳台最大宽度。
        private const double PlatformMinDist = 14; // 跳台最小间距。
        private const double PlatformMaxDist = 192; // 跳台最大间距。

        private static readonly Com.PointD3D CharacterSize = new Com.PointD3D(14, 14, 28); // 角色大小。

        private double MaxJumpDistance => 4 * (PlatformMaxWidth + PlatformMaxDist); // 最大跳跃距离。

        private double JumpVelocity => PressSensitivity * 0.01 * 320; // 跳跃距离与按下持续秒数的比值。

        private const double MinPressDownSeconds = 0.01; // 最小按下持续秒数。
        private double MaxPressDownSeconds => MaxJumpDistance / JumpVelocity; // 最大按下持续秒数。

        private struct Cuboid // 长方体。
        {
            public static readonly Cuboid Empty = new Cuboid() // 表示所有成员为 Empty 的长方体。
            {
                Center = Com.PointD3D.Zero,
                Size = Com.PointD3D.Zero,
                Color = Color.Empty
            };

            //

            public bool IsEmpty // 判断此长方体是否为 Empty。
            {
                get
                {
                    if (Center == Empty.Center && Size == Empty.Size && Color == Empty.Color)
                    {
                        return true;
                    }

                    return false;
                }
            }

            //

            public Com.PointD3D Center; // 中心坐标。
            public Com.PointD3D Size; // 大小。
            public Color Color; // 颜色。

            //

            public bool Painted; // 此长方体是否已经被绘制。
        }

        //

        private List<Cuboid> PlatformList = new List<Cuboid>(6); // 跳台列表。

        private Cuboid Character = Cuboid.Empty; // 角色。

        //

        private DateTime GameStartingTime = DateTime.Now; // 本次游戏开始时刻。

        private TimeSpan ThisGameTime = TimeSpan.Zero; // 本次游戏时长。
        private TimeSpan TotalGameTime = TimeSpan.Zero; // 累计游戏时长。

        //

        private Size GameBmpSize = new Size(600, 600); // 游戏位图大小。

        //

        private struct Record // 记录。
        {
            public double Score; // 得分。
            public Int64 PlatformCount; // 跳台数量。
            public double Accuracy; // 准确度（平均值）。
            public double LastExtraScore; // 最后的额外分数。
            public Directions NextDirection; // 下一步的方向。
        }

        private Record BestRecord = new Record(); // 最高分记录。
        private Record ThisRecord = new Record(); // 本次记录。

        //

        private Record Record_Last = new Record(); // 上次游戏的记录。

        private List<Cuboid> PlatformList_Last = new List<Cuboid>(3); // 上次游戏的跳台列表。

        private Cuboid Character_Last = Cuboid.Empty; // 上次游戏的角色。

        #endregion

        #region 窗体构造

        private Com.WinForm.FormManager Me;

        public Com.WinForm.FormManager FormManager
        {
            get
            {
                return Me;
            }
        }

        private void _Ctor(Com.WinForm.FormManager owner)
        {
            InitializeComponent();

            //

            if (owner != null)
            {
                Me = new Com.WinForm.FormManager(this, owner);
            }
            else
            {
                Me = new Com.WinForm.FormManager(this);
            }

            //

            FormDefine();
        }

        public Form_Main()
        {
            _Ctor(null);
        }

        public Form_Main(Com.WinForm.FormManager owner)
        {
            _Ctor(owner);
        }

        private void FormDefine()
        {
            Me.Caption = ApplicationName;
            Me.FormStyle = Com.WinForm.FormStyle.Sizable;
            Me.EnableFullScreen = true;
            Me.ClientSize = FormClientInitialSize;
            Me.Theme = Theme_DEFAULT;
            Me.ThemeColor = new Com.ColorX(ThemeColor_DEFAULT);
            Me.ShowCaptionBarColor = ShowFormTitleColor_DEFAULT;

            Me.Loading += LoadingEvents;
            Me.Loaded += LoadedEvents;
            Me.Closed += ClosedEvents;
            Me.Resize += ResizeEvents;
            Me.SizeChanged += SizeChangedEvents;
            Me.ThemeChanged += ThemeColorChangedEvents;
            Me.ThemeColorChanged += ThemeColorChangedEvents;
        }

        #endregion

        #region 窗体事件

        private void LoadingEvents(object sender, EventArgs e)
        {
            //
            // 在窗体加载时发生。
            //

            TransConfig();

            DelOldConfig();

            LoadConfig();

            LoadUserData();

            LoadLastGame();

            //

            if (UseRandomThemeColor)
            {
                Me.ThemeColor = Com.ColorManipulation.GetRandomColorX();
            }
        }

        private void LoadedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体加载后发生。
            //

            Me.OnSizeChanged();
            Me.OnThemeChanged();

            //

            RadioButton_UseRandomThemeColor.CheckedChanged -= RadioButton_UseRandomThemeColor_CheckedChanged;
            RadioButton_UseCustomColor.CheckedChanged -= RadioButton_UseCustomColor_CheckedChanged;

            if (UseRandomThemeColor)
            {
                RadioButton_UseRandomThemeColor.Checked = true;
            }
            else
            {
                RadioButton_UseCustomColor.Checked = true;
            }

            RadioButton_UseRandomThemeColor.CheckedChanged += RadioButton_UseRandomThemeColor_CheckedChanged;
            RadioButton_UseCustomColor.CheckedChanged += RadioButton_UseCustomColor_CheckedChanged;

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;

            //

            CheckBox_AntiAlias.CheckedChanged -= CheckBox_AntiAlias_CheckedChanged;

            CheckBox_AntiAlias.Checked = AntiAlias;

            CheckBox_AntiAlias.CheckedChanged += CheckBox_AntiAlias_CheckedChanged;

            //

            Label_ApplicationName.Text = ApplicationName;
            Label_ApplicationEdition.Text = ApplicationEdition;
            Label_Version.Text = "版本: " + MajorVersion + "." + MinorVersion + "." + BuildNumber + "." + BuildRevision;

            //

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_StartNewGame, Label_StartNewGame_Click);
            Com.WinForm.ControlSubstitution.LabelAsButton(Label_ContinueLastGame, Label_ContinueLastGame_Click);

            //

            FunctionAreaTab = FunctionAreaTabs.Start;
        }

        private void ClosedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体关闭后发生。
            //

            SaveConfig();

            if (GameUINow)
            {
                Interrupt(InterruptActions.CloseApp);
            }
        }

        private void ResizeEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小调整时发生。
            //

            Panel_FunctionArea.Size = Panel_GameUI.Size = Panel_Client.Size = Panel_Main.Size;

            Panel_FunctionAreaOptionsBar.Size = new Size(Panel_FunctionArea.Width / 3, Panel_FunctionArea.Height);
            Label_Tab_Start.Size = Label_Tab_Record.Size = Label_Tab_Options.Size = Label_Tab_About.Size = new Size(Panel_FunctionAreaOptionsBar.Width, Panel_FunctionAreaOptionsBar.Height / 4);
            Label_Tab_Record.Top = Label_Tab_Start.Bottom;
            Label_Tab_Options.Top = Label_Tab_Record.Bottom;
            Label_Tab_About.Top = Label_Tab_Options.Bottom;

            Panel_FunctionAreaTab.Left = Panel_FunctionAreaOptionsBar.Right;
            Panel_FunctionAreaTab.Size = new Size(Panel_FunctionArea.Width - Panel_FunctionAreaOptionsBar.Width, Panel_FunctionArea.Height);

            Func<Control, Control, Size> GetTabSize = (Tab, Container) => new Size(Container.Width - (Container.Height < Tab.MinimumSize.Height ? 25 : 0), Container.Height - (Container.Width < Tab.MinimumSize.Width ? 25 : 0));

            Panel_Tab_Start.Size = GetTabSize(Panel_Tab_Start, Panel_FunctionAreaTab);
            Panel_Tab_Record.Size = GetTabSize(Panel_Tab_Record, Panel_FunctionAreaTab);
            Panel_Tab_Options.Size = GetTabSize(Panel_Tab_Options, Panel_FunctionAreaTab);
            Panel_Tab_About.Size = GetTabSize(Panel_Tab_About, Panel_FunctionAreaTab);

            //

            Panel_EnterGameSelection.Location = new Point((Panel_Tab_Start.Width - Panel_EnterGameSelection.Width) / 2, (Panel_Tab_Start.Height - Panel_EnterGameSelection.Height) / 2);

            Panel_Score.Width = Panel_Tab_Record.Width - Panel_Score.Left * 2;
            Panel_Score.Height = Panel_Tab_Record.Height - Panel_Score.Top * 2 - Panel_GameTime.Height;
            Panel_GameTime.Width = Panel_Tab_Record.Width - Panel_GameTime.Left * 2;
            Panel_GameTime.Top = Panel_Score.Bottom;
            Label_ThisRecord.Location = new Point(Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecord.Width, (Panel_Score.Width / 2 - Label_ThisRecord.Width) / 2)), Panel_Score.Height - 25 - Label_ThisRecord.Height);
            Label_BestRecord.Location = new Point(Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecord.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecord.Width) / 2)), Panel_Score.Height - 25 - Label_BestRecord.Height);

            Panel_PressSensitivity.Width = Panel_Tab_Options.Width - Panel_ThemeColor.Left * 2;
            Panel_PlatformOpacity.Width = Panel_Tab_Options.Width - Panel_ThemeColor.Left * 2;
            Panel_ThemeColor.Width = Panel_Tab_Options.Width - Panel_ThemeColor.Left * 2;
            Panel_AntiAlias.Width = Panel_Tab_Options.Width - Panel_AntiAlias.Left * 2;

            //

            Panel_Current.Width = Panel_GameUI.Width;

            Panel_Interrupt.Left = Panel_Current.Width - Panel_Interrupt.Width;

            Panel_Environment.Size = new Size(Panel_GameUI.Width, Panel_GameUI.Height - Panel_Environment.Top);
        }

        private void SizeChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的大小更改时发生。
            //

            if (Panel_GameUI.Visible)
            {
                Int32 Sz = Math.Min(Panel_Environment.Width, Panel_Environment.Height);

                GameBmpSize = new Size(Sz, Sz);

                GameBmpRect.Location = new Point((Panel_Environment.Width - GameBmpSize.Width) / 2, (Panel_Environment.Height - GameBmpSize.Height) / 2);
                GameBmpRect.Size = GameBmpSize;

                RepaintCurBmp();

                RepaintGameBmp();
            }

            if (Panel_FunctionArea.Visible && FunctionAreaTab == FunctionAreaTabs.Record)
            {
                Panel_Tab_Record.Refresh();
            }
        }

        private void ThemeColorChangedEvents(object sender, EventArgs e)
        {
            //
            // 在窗体的主题色更改时发生。
            //

            // 功能区选项卡

            Panel_FunctionArea.BackColor = Me.RecommendColors.Background_DEC.ToColor();
            Panel_FunctionAreaOptionsBar.BackColor = Me.RecommendColors.Main.ToColor();

            FunctionAreaTab = _FunctionAreaTab;

            // "记录"区域

            Label_ThisRecord.ForeColor = Label_BestRecord.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_ThisRecordVal_Score.ForeColor = Label_BestRecordVal_Score.ForeColor = Me.RecommendColors.Text_INC.ToColor();
            Label_ThisRecordVal_PlatformCountAndAccuracy.ForeColor = Label_BestRecordVal_PlatformCountAndAccuracy.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_ThisTime.ForeColor = Label_TotalTime.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_ThisTimeVal.ForeColor = Label_TotalTimeVal.ForeColor = Me.RecommendColors.Text_INC.ToColor();

            // "选项"区域

            Label_PressSensitivity.ForeColor = Label_PlatformOpacity.ForeColor = Label_ThemeColor.ForeColor = Label_AntiAlias.ForeColor = Me.RecommendColors.Text_INC.ToColor();

            Label_PressSensitivity_Val.ForeColor = Me.RecommendColors.Text.ToColor();

            Panel_PressSensitivityAdjustment.BackColor = Panel_FunctionArea.BackColor;

            Label_PlatformOpacity_Val.ForeColor = Me.RecommendColors.Text.ToColor();

            Panel_PlatformOpacityAdjustment.BackColor = Panel_FunctionArea.BackColor;

            RadioButton_UseRandomThemeColor.ForeColor = RadioButton_UseCustomColor.ForeColor = Me.RecommendColors.Text.ToColor();

            Label_ThemeColorName.Text = Com.ColorManipulation.GetColorName(Me.ThemeColor.ToColor());
            Label_ThemeColorName.ForeColor = Me.RecommendColors.Text.ToColor();

            CheckBox_AntiAlias.ForeColor = Me.RecommendColors.Text.ToColor();

            // "关于"区域

            Label_ApplicationName.ForeColor = Me.RecommendColors.Text_INC.ToColor();
            Label_ApplicationEdition.ForeColor = Label_Version.ForeColor = Label_Copyright.ForeColor = Me.RecommendColors.Text.ToColor();
            Label_GitHub_Part1.ForeColor = Label_GitHub_Base.ForeColor = Label_GitHub_Part2.ForeColor = Label_GitHub_Release.ForeColor = Me.RecommendColors.Text.ToColor();

            // 控件替代

            Com.WinForm.ControlSubstitution.PictureBoxAsButton(PictureBox_Restart, PictureBox_Restart_Click, null, PictureBox_Restart_MouseEnter, null, Color.Transparent, Me.RecommendColors.Button_INC.AtOpacity(50).ToColor(), Me.RecommendColors.Button_INC.AtOpacity(70).ToColor());
            Com.WinForm.ControlSubstitution.PictureBoxAsButton(PictureBox_ExitGame, PictureBox_ExitGame_Click, null, PictureBox_ExitGame_MouseEnter, null, Color.Transparent, Me.RecommendColors.Button_INC.AtOpacity(50).ToColor(), Me.RecommendColors.Button_INC.AtOpacity(70).ToColor());

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_ThemeColorName, Label_ThemeColorName_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));

            Com.WinForm.ControlSubstitution.LabelAsButton(Label_GitHub_Base, Label_GitHub_Base_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));
            Com.WinForm.ControlSubstitution.LabelAsButton(Label_GitHub_Release, Label_GitHub_Release_Click, Color.Transparent, Me.RecommendColors.Button_DEC.ToColor(), Me.RecommendColors.Button_INC.ToColor(), new Font("微软雅黑", 9.75F, FontStyle.Underline, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134), new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134));

            // 中断按钮图像

            InterruptImages.Update(Me.RecommendColors.Text.ToColor());

            PictureBox_Restart.Image = InterruptImages.Restart;
            PictureBox_ExitGame.Image = InterruptImages.ExitGame;
        }

        #endregion

        #region 背景绘图

        private void Panel_FunctionAreaOptionsBar_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_FunctionAreaOptionsBar 绘图。
            //

            Graphics Grap = e.Graphics;
            Grap.SmoothingMode = SmoothingMode.AntiAlias;

            //

            Control[] TabCtrl = new Control[(Int32)FunctionAreaTabs.COUNT] { Label_Tab_Start, Label_Tab_Record, Label_Tab_Options, Label_Tab_About };

            List<bool> TabBtnPointed = new List<bool>(TabCtrl.Length);
            List<bool> TabBtnSeld = new List<bool>(TabCtrl.Length);

            for (int i = 0; i < TabCtrl.Length; i++)
            {
                TabBtnPointed.Add(Com.Geometry.CursorIsInControl(TabCtrl[i]));
                TabBtnSeld.Add(FunctionAreaTab == (FunctionAreaTabs)i);
            }

            Color TabBtnCr_Bk_Pointed = Color.FromArgb(128, Color.White), TabBtnCr_Bk_Seld = Color.FromArgb(192, Color.White), TabBtnCr_Bk_Uns = Color.FromArgb(64, Color.White);

            for (int i = 0; i < TabCtrl.Length; i++)
            {
                Color TabBtnCr_Bk = (TabBtnSeld[i] ? TabBtnCr_Bk_Seld : (TabBtnPointed[i] ? TabBtnCr_Bk_Pointed : TabBtnCr_Bk_Uns));

                GraphicsPath Path_TabBtn = new GraphicsPath();
                Path_TabBtn.AddRectangle(TabCtrl[i].Bounds);
                PathGradientBrush PGB_TabBtn = new PathGradientBrush(Path_TabBtn)
                {
                    CenterColor = Color.FromArgb(TabBtnCr_Bk.A / 2, TabBtnCr_Bk),
                    SurroundColors = new Color[] { TabBtnCr_Bk },
                    FocusScales = new PointF(1F, 0F)
                };
                Grap.FillPath(PGB_TabBtn, Path_TabBtn);
                Path_TabBtn.Dispose();
                PGB_TabBtn.Dispose();

                if (TabBtnSeld[i])
                {
                    PointF[] Polygon = new PointF[] { new PointF(TabCtrl[i].Right, TabCtrl[i].Top + TabCtrl[i].Height / 4), new PointF(TabCtrl[i].Right - TabCtrl[i].Height / 4, TabCtrl[i].Top + TabCtrl[i].Height / 2), new PointF(TabCtrl[i].Right, TabCtrl[i].Bottom - TabCtrl[i].Height / 4) };

                    Grap.FillPolygon(new SolidBrush(Panel_FunctionArea.BackColor), Polygon);
                }
            }
        }

        private void Panel_Score_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Score 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = PictureBox_Score;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }

            //

            PaintScore(e);
        }

        private void Panel_GameTime_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_GameTime 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = PictureBox_GameTime;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_PressSensitivity_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_PressSensitivity 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_PressSensitivity;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_PlatformOpacity_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_PlatformOpacity 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_PlatformOpacity;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_ThemeColor_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_ThemeColor 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_ThemeColor;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        private void Panel_AntiAlias_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_AntiAlias 绘图。
            //

            Control Cntr = sender as Control;

            if (Cntr != null)
            {
                Pen P = new Pen(Me.RecommendColors.Border_DEC.ToColor(), 1);
                Control Ctrl = Label_AntiAlias;
                e.Graphics.DrawLine(P, new Point(Ctrl.Right, Ctrl.Top + Ctrl.Height / 2), new Point(Cntr.Width - Ctrl.Left, Ctrl.Top + Ctrl.Height / 2));
                P.Dispose();
            }
        }

        #endregion

        #region 配置设置

        private void TransConfig()
        {
            //
            // 从当前内部版本号下最近的旧版本迁移配置文件。
            //

            try
            {
                if (!Directory.Exists(RootDir_CurrentVersion))
                {
                    if (OldVersionList.Count > 0)
                    {
                        List<Version> OldVersionList_Copy = new List<Version>(OldVersionList);
                        List<Version> OldVersionList_Sorted = new List<Version>(OldVersionList_Copy.Count);

                        while (OldVersionList_Copy.Count > 0)
                        {
                            Version LatestVersion = OldVersionList_Copy[0];

                            foreach (Version Ver in OldVersionList_Copy)
                            {
                                if (LatestVersion <= Ver)
                                {
                                    LatestVersion = Ver;
                                }
                            }

                            OldVersionList_Sorted.Add(LatestVersion);
                            OldVersionList_Copy.Remove(LatestVersion);
                        }

                        for (int i = 0; i < OldVersionList_Sorted.Count; i++)
                        {
                            string Dir = RootDir_Product + "\\" + OldVersionList_Sorted[i].Build + "." + OldVersionList_Sorted[i].Revision;

                            if (Directory.Exists(Dir))
                            {
                                try
                                {
                                    Com.IO.CopyFolder(Dir, RootDir_CurrentVersion, true, true, true);

                                    break;
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void DelOldConfig()
        {
            //
            // 删除当前内部版本号下所有旧版本的配置文件。
            //

            try
            {
                if (OldVersionList.Count > 0)
                {
                    foreach (Version Ver in OldVersionList)
                    {
                        string Dir = RootDir_Product + "\\" + Ver.Build + "." + Ver.Revision;

                        if (Directory.Exists(Dir))
                        {
                            try
                            {
                                Directory.Delete(Dir, true);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadConfig()
        {
            //
            // 加载配置文件。
            //

            if (File.Exists(ConfigFilePath))
            {
                if (new FileInfo(ConfigFilePath).Length > 0)
                {
                    StreamReader Read = new StreamReader(ConfigFilePath, false);
                    string Cfg = Read.ReadLine();
                    Read.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");
                    Regex RegexInt = new Regex(@"[^0-9\-]");
                    Regex RegexFloat = new Regex(@"[^0-9\-\.]");

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Cfg, "<GameBmpSize>", "</GameBmpSize>", false, false);

                        string[] Fields = SubStr.Split(',');

                        if (Fields.Length == 2)
                        {
                            int i = 0;

                            string StrW = RegexUint.Replace(Fields[i++], string.Empty);
                            string StrH = RegexUint.Replace(Fields[i++], string.Empty);

                            GameBmpSize = new Size(Convert.ToInt32(StrW), Convert.ToInt32(StrH));
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = RegexInt.Replace(Com.Text.GetIntervalString(Cfg, "<PressSensitivity>", "</PressSensitivity>", false, false), string.Empty);

                        Int32 PS = Convert.ToInt32(SubStr);

                        if (PS >= PressSensitivity_MIN && PS <= PressSensitivity_MAX)
                        {
                            PressSensitivity = PS;
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = RegexInt.Replace(Com.Text.GetIntervalString(Cfg, "<PlatformOpacity>", "</PlatformOpacity>", false, false), string.Empty);

                        Int32 PO = Convert.ToInt32(SubStr);

                        if (PO >= PlatformOpacity_MIN && PO <= PlatformOpacity_MAX)
                        {
                            PlatformOpacity = PO;
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Cfg, "<Theme>", "</Theme>", false, false);

                        foreach (object Obj in Enum.GetValues(typeof(Com.WinForm.Theme)))
                        {
                            if (SubStr.Trim().ToUpper() == Obj.ToString().ToUpper())
                            {
                                Me.Theme = (Com.WinForm.Theme)Obj;

                                break;
                            }
                        }
                    }
                    catch { }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<UseRandomThemeColor>", "</UseRandomThemeColor>", false, false).Contains((!UseRandomThemeColor).ToString()))
                    {
                        UseRandomThemeColor = !UseRandomThemeColor;
                    }

                    if (!UseRandomThemeColor)
                    {
                        try
                        {
                            string SubStr = Com.Text.GetIntervalString(Cfg, "<ThemeColor>", "</ThemeColor>", false, false);

                            string[] Fields = SubStr.Split(',');

                            if (Fields.Length == 3)
                            {
                                int i = 0;

                                string StrR = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_R = Convert.ToInt32(StrR);

                                string StrG = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_G = Convert.ToInt32(StrG);

                                string StrB = RegexUint.Replace(Fields[i++], string.Empty);
                                Int32 TC_B = Convert.ToInt32(StrB);

                                Me.ThemeColor = Com.ColorX.FromRGB(TC_R, TC_G, TC_B);
                            }
                        }
                        catch { }
                    }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<ShowFormTitleColor>", "</ShowFormTitleColor>", false, false).Contains((!Me.ShowCaptionBarColor).ToString()))
                    {
                        Me.ShowCaptionBarColor = !Me.ShowCaptionBarColor;
                    }

                    //

                    try
                    {
                        string SubStr = RegexFloat.Replace(Com.Text.GetIntervalString(Cfg, "<Opacity>", "</Opacity>", false, false), string.Empty);

                        double Op = Convert.ToDouble(SubStr);

                        if (Op >= Opacity_MIN && Op <= Opacity_MAX)
                        {
                            Me.Opacity = Op;
                        }
                    }
                    catch { }

                    //

                    if (Com.Text.GetIntervalString(Cfg, "<AntiAlias>", "</AntiAlias>", false, false).Contains((!AntiAlias).ToString()))
                    {
                        AntiAlias = !AntiAlias;
                    }
                }
            }
        }

        private void SaveConfig()
        {
            //
            // 保存配置文件。
            //

            string Cfg = string.Empty;

            Cfg += "<Config>";

            Cfg += "<GameBmpSize>(" + GameBmpSize.Width + "," + GameBmpSize.Height + ")</GameBmpSize>";
            Cfg += "<PressSensitivity>" + PressSensitivity + "</PressSensitivity>";
            Cfg += "<PlatformOpacity>" + PlatformOpacity + "</PlatformOpacity>";

            Cfg += "<Theme>" + Me.Theme.ToString() + "</Theme>";
            Cfg += "<UseRandomThemeColor>" + UseRandomThemeColor + "</UseRandomThemeColor>";
            Cfg += "<ThemeColor>(" + Me.ThemeColor.ToColor().R + ", " + Me.ThemeColor.ToColor().G + ", " + Me.ThemeColor.ToColor().B + ")</ThemeColor>";
            Cfg += "<ShowFormTitleColor>" + Me.ShowCaptionBarColor + "</ShowFormTitleColor>";
            Cfg += "<Opacity>" + Me.Opacity + "</Opacity>";

            Cfg += "<AntiAlias>" + AntiAlias + "</AntiAlias>";

            Cfg += "</Config>";

            //

            try
            {
                if (!Directory.Exists(ConfigFileDir))
                {
                    Directory.CreateDirectory(ConfigFileDir);
                }

                StreamWriter Write = new StreamWriter(ConfigFilePath, false);
                Write.WriteLine(Cfg);
                Write.Close();
            }
            catch { }
        }

        #endregion

        #region 存档管理

        // 用户数据。

        private void LoadUserData()
        {
            //
            // 加载用户数据。
            //

            if (File.Exists(DataFilePath))
            {
                FileInfo FInfo = new FileInfo(DataFilePath);

                if (FInfo.Length > 0)
                {
                    StreamReader SR = new StreamReader(DataFilePath, false);
                    string Str = SR.ReadLine();
                    SR.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");
                    Regex RegexFloatExp = new Regex(@"[^0-9E\+\-\.]");

                    //

                    try
                    {
                        string SubStr = RegexUint.Replace(Com.Text.GetIntervalString(Str, "<TotalGameTime>", "</TotalGameTime>", false, false), string.Empty);

                        TotalGameTime = TimeSpan.FromMilliseconds(Convert.ToInt64(SubStr));
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<BestRecord>", "</BestRecord>", false, false);

                        string[] Fields = SubStr.Split(',');

                        if (Fields.Length == 3 || Fields.Length == 2)
                        {
                            int i = 0;

                            Record Rec = new Record();

                            if (Fields.Length == 3)
                            {
                                string StrScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Score = Convert.ToDouble(StrScore);

                                string StrPC = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.PlatformCount = Convert.ToInt32(StrPC);

                                string StrAccuracy = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Accuracy = Convert.ToDouble(StrAccuracy);
                            }
                            else if (Fields.Length == 2)
                            {
                                string StrScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Score = Convert.ToDouble(StrScore);

                                string StrPC = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.PlatformCount = Convert.ToInt32(StrPC);
                            }

                            if (Rec.Score >= 0 && Rec.PlatformCount >= 0 && (Rec.Accuracy >= 0 && Rec.Accuracy <= 1))
                            {
                                BestRecord = Rec;
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void SaveUserData()
        {
            //
            // 保存用户数据。
            //

            if (BestRecord.Score < ThisRecord.Score || (BestRecord.Score == ThisRecord.Score && BestRecord.PlatformCount > ThisRecord.PlatformCount) || (BestRecord.Score == ThisRecord.Score && BestRecord.PlatformCount == ThisRecord.PlatformCount && BestRecord.Accuracy < ThisRecord.Accuracy))
            {
                BestRecord = ThisRecord;
            }

            //

            string Str = string.Empty;

            Str += "<Log>";

            Str += "<TotalGameTime>" + (Int64)TotalGameTime.TotalMilliseconds + "</TotalGameTime>";

            Str += "<BestRecord>(" + BestRecord.Score + "," + BestRecord.PlatformCount + "," + BestRecord.Accuracy + ")</BestRecord>";

            Str += "</Log>";

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(DataFilePath, false);
                SW.WriteLine(Str);
                SW.Close();
            }
            catch { }
        }

        // 上次游戏。

        private void LoadLastGame()
        {
            //
            // 加载上次游戏。
            //

            if (File.Exists(RecordFilePath))
            {
                FileInfo FInfo = new FileInfo(RecordFilePath);

                if (FInfo.Length > 0)
                {
                    StreamReader SR = new StreamReader(RecordFilePath, false);
                    string Str = SR.ReadLine();
                    SR.Close();

                    Regex RegexUint = new Regex(@"[^0-9]");
                    Regex RegexInt = new Regex(@"[^0-9\-]");
                    Regex RegexFloatExp = new Regex(@"[^0-9E\+\-\.]");

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<Platform>", "</Platform>", false, false);

                        while (SubStr.Contains("(") && SubStr.Contains(")"))
                        {
                            try
                            {
                                string StrE = Com.Text.GetIntervalString(SubStr, "(", ")", false, false);

                                string[] Fields = StrE.Split(',');

                                if (Fields.Length == 6)
                                {
                                    int i = 0;

                                    Cuboid P = new Cuboid();

                                    string StrCenX = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                    P.Center.X = Convert.ToDouble(StrCenX);

                                    string StrCenY = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                    P.Center.Y = Convert.ToDouble(StrCenY);

                                    string StrCenZ = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                    P.Center.Z = Convert.ToDouble(StrCenZ);

                                    string StrSzX = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                    P.Size.X = Convert.ToDouble(StrSzX);

                                    string StrSzY = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                    P.Size.Y = Convert.ToDouble(StrSzY);

                                    string StrSCr = RegexInt.Replace(Fields[i++], string.Empty);
                                    P.Color = Color.FromArgb(Convert.ToInt32(StrSCr));

                                    if (P.Size.X >= PlatformMinWidth && P.Size.X <= PlatformMaxWidth && P.Size.Y >= PlatformMinWidth && P.Size.Y <= PlatformMaxWidth)
                                    {
                                        P.Size.Z = PlatformHeight;

                                        PlatformList_Last.Add(P);
                                    }
                                }
                            }
                            catch { }

                            SubStr = SubStr.Substring(SubStr.IndexOf(")") + (")").Length);
                        }
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = RegexInt.Replace(Com.Text.GetIntervalString(Str, "<Character>", "</Character>", false, false), string.Empty);

                        Character_Last.Color = Color.FromArgb(Convert.ToInt32(SubStr));
                    }
                    catch { }

                    //

                    try
                    {
                        string SubStr = Com.Text.GetIntervalString(Str, "<Record>", "</Record>", false, false);

                        string[] Fields = SubStr.Split(',');

                        if (Fields.Length == 5 || Fields.Length == 4)
                        {
                            int i = 0;

                            Record Rec = new Record();

                            if (Fields.Length == 5)
                            {
                                string StrScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Score = Convert.ToDouble(StrScore);

                                string StrPC = RegexUint.Replace(Fields[i++], string.Empty);
                                Rec.PlatformCount = Convert.ToInt64(StrPC);

                                string StrAccuracy = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Accuracy = Convert.ToDouble(StrAccuracy);

                                string StrLEScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.LastExtraScore = Convert.ToDouble(StrLEScore);

                                string StrLD = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.NextDirection = (Directions)Convert.ToInt32(StrLD);
                            }
                            else if (Fields.Length == 4)
                            {
                                string StrScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.Score = Convert.ToDouble(StrScore);

                                string StrPC = RegexUint.Replace(Fields[i++], string.Empty);
                                Rec.PlatformCount = Convert.ToInt64(StrPC);

                                string StrLEScore = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.LastExtraScore = Convert.ToDouble(StrLEScore);

                                string StrLD = RegexFloatExp.Replace(Fields[i++], string.Empty);
                                Rec.NextDirection = (Directions)Convert.ToInt32(StrLD);
                            }

                            if (Rec.Score >= 0 && Rec.PlatformCount >= 0 && (Rec.Accuracy >= 0 && Rec.Accuracy <= 1))
                            {
                                Record_Last = Rec;
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void SaveLastGame()
        {
            //
            // 保存上次游戏。
            //

            ThisRecord.NextDirection = NextDirection;

            Record_Last = ThisRecord;

            PlatformList_Last.Clear();
            PlatformList_Last.AddRange(PlatformList);

            Character_Last.Color = Character.Color;

            //

            string Str = string.Empty;

            Str += "<Log>";

            Str += "<Platform>[";
            for (int i = 0; i < PlatformList.Count; i++)
            {
                Cuboid P = PlatformList[i];

                Str += "(" + P.Center.X + "," + P.Center.Y + "," + P.Center.Z + "," + P.Size.X + "," + P.Size.Y + "," + P.Color.ToArgb() + ")";
            }
            Str += "]</Platform>";

            Str += "<Character>(" + Character.Color.ToArgb() + ")</Character>";

            Str += "<Record>(" + ThisRecord.Score + "," + ThisRecord.PlatformCount + "," + ThisRecord.Accuracy + "," + ThisRecord.LastExtraScore + "," + (Int32)ThisRecord.NextDirection + ")</Record>";

            Str += "</Log>";

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(RecordFilePath, false);
                SW.WriteLine(Str);
                SW.Close();
            }
            catch { }
        }

        private void EraseLastGame()
        {
            //
            // 擦除上次游戏。
            //

            PlatformList_Last.Clear();

            Character_Last = Cuboid.Empty;

            Record_Last = new Record();

            //

            try
            {
                if (!Directory.Exists(LogFileDir))
                {
                    Directory.CreateDirectory(LogFileDir);
                }

                StreamWriter SW = new StreamWriter(RecordFilePath, false);
                SW.WriteLine(string.Empty);
                SW.Close();
            }
            catch { }
        }

        #endregion

        #region 3D 绘图与逻辑操作

        private Cuboid LastPlatform // 跳台列表中的最后一个元素。 
        {
            get
            {
                if (PlatformList.Count > 0)
                {
                    return PlatformList[PlatformList.Count - 1];
                }

                return Cuboid.Empty;
            }

            set
            {
                if (PlatformList.Count > 0)
                {
                    PlatformList[PlatformList.Count - 1] = value;
                }
            }
        }

        private Cuboid CurrentPlatform // 角色当前所在的跳台。
        {
            get
            {
                if (PlatformList.Count > 0)
                {
                    for (int i = PlatformList.Count - 1; i >= 0; i--)
                    {
                        Cuboid P = PlatformList[i];

                        if (Com.Geometry.PointIsVisibleInRectangle(Character.Center.XY, new RectangleF((P.Center - P.Size / 2).XY.ToPointF(), P.Size.XY.ToSizeF())))
                        {
                            return P;
                        }
                    }
                }

                return Cuboid.Empty;
            }

            set
            {
                if (PlatformList.Count > 0)
                {
                    for (int i = PlatformList.Count - 1; i >= 0; i--)
                    {
                        Cuboid P = PlatformList[i];

                        if (Com.Geometry.PointIsVisibleInRectangle(Character.Center.XY, new RectangleF((P.Center - P.Size / 2).XY.ToPointF(), P.Size.XY.ToSizeF())))
                        {
                            PlatformList[i] = value;
                        }
                    }
                }
            }
        }

        private double Difficulty => (ThisRecord.Score > 0 ? 1 - Math.Pow(2, -ThisRecord.Score * (1 - Math.Pow(1 - ThisRecord.PlatformCount / ThisRecord.Score, 2) / 2) / 128) : 0); // 难度系数。

        private Com.PointD ExtraScoreAreaSize => (Com.PointD3D.Min(LastPlatform.Size / 2, CharacterSize)).XY; // 能够获得额外分数的区域大小。

        // 绘图。

        private double GraphScale => Math.Min(GameBmpRect.Width, GameBmpRect.Height) / (Math.Sqrt(3) * (PlatformMaxWidth + PlatformMaxDist)); // 绘图缩放比例。

        private double TrueLenDist => new Com.PointD(Screen.PrimaryScreen.Bounds.Size).Module * GraphScale; // 真实尺寸距离。

        private Com.Matrix GraphAffineMatrix => new Com.Matrix(new double[4, 4] // 绘图仿射矩阵。此仿射矩阵表示将三维坐标依次绕 X 轴旋转 90°、绕 Y 轴旋转 -45°、绕 X 轴旋转 35°，以及平移至特定位置。
        {
            { 0.70710678118654752, -0.4055797876726388, 0.57922796533956917, 0 },
            { -0.70710678118654752, -0.4055797876726388, 0.57922796533956917, 0 },
            { 0, -0.8191520442889918, -0.573576436351046, 0 },
            { GameBmpRect.Width / 2, GameBmpRect.Height / 2, TrueLenDist, 1 }
        });

        private List<Com.Matrix> GraphAffineMatrixList = new List<Com.Matrix>(4); // 绘图仿射矩阵列表。
        private List<Com.Matrix> CharacterAffineMatrixList = new List<Com.Matrix>(3); // 角色仿射矩阵列表。

        private Com.PointD3D IlluminationDirection // 光照方向。
        {
            get
            {
                Com.Matrix GAMatrix = GraphAffineMatrix.Copy();

                if (!Com.Matrix.IsNullOrEmpty(GAMatrix) && GAMatrix.Size == new Size(4, 4))
                {
                    GAMatrix[3, 0] = GAMatrix[3, 1] = GAMatrix[3, 2] = 0;

                    return new Com.PointD3D(1, Math.Sqrt(2), -2).AffineTransformCopy(GAMatrix);
                }

                return Com.PointD3D.Zero;
            }
        }

        private const double Exposure = 0; // 曝光。

        private Rectangle GameBmpRect = new Rectangle(); // 游戏位图区域（相对于绘图容器）。

        private Bitmap GameBmp; // 游戏位图。

        private void UpdateGameBmp()
        {
            //
            // 更新游戏位图。
            //

            if (GameBmp != null)
            {
                GameBmp.Dispose();
            }

            GameBmp = new Bitmap(Math.Max(1, GameBmpRect.Width), Math.Max(1, GameBmpRect.Height));

            using (Graphics GameBmpGrap = Graphics.FromImage(GameBmp))
            {
                if (AntiAlias)
                {
                    GameBmpGrap.SmoothingMode = SmoothingMode.AntiAlias;
                }

                GameBmpGrap.Clear(GameUIBackColor_INC);

                //

                Func<Color, Int32> GetAlpha = (Cr) => Math.Max(0, Math.Min((Int32)(PlatformOpacity * 0.01 * Cr.A), 255));

                double GScale = GraphScale;
                double TLDist = TrueLenDist;
                Com.PointD3D IDirection = IlluminationDirection;

                Com.Matrix GAMatrix = Com.Matrix.MultiplyLeft(GraphAffineMatrixList);

                if (Com.Matrix.IsNullOrEmpty(GAMatrix))
                {
                    GAMatrix = GraphAffineMatrix;
                }

                for (int i = PlatformList.Count - 1; i >= 0; i--)
                {
                    Cuboid P = PlatformList[i];

                    P.Painted = Com.Painting3D.PaintCuboid(GameBmp, P.Center * GScale, P.Size * GScale, Color.FromArgb(GetAlpha(P.Color), (GameIsOver ? Com.ColorManipulation.GetGrayscaleColor(P.Color) : P.Color)), (float)GScale, GAMatrix, TLDist, IDirection, true, Exposure, AntiAlias);

                    PlatformList[i] = P;
                }

                if (ThisRecord.LastExtraScore > 0)
                {
                    Cuboid LP = LastPlatform;
                    Com.PointD ESASize = ExtraScoreAreaSize;

                    Cuboid ESA = new Cuboid()
                    {
                        Center = new Com.PointD3D(LP.Center.X, LP.Center.Y, LP.Center.Z + LP.Size.Z / 2),
                        Size = Com.PointD3D.Min(LP.Size, new Com.PointD3D(ESASize.X + CharacterSize.X, ESASize.Y + CharacterSize.Y, 0)),
                        Color = Color.FromArgb(128, Me.Theme <= Com.WinForm.Theme.LightGray ? Color.White : Color.Black)
                    };

                    Com.Painting3D.PaintCuboid(GameBmp, ESA.Center * GScale, ESA.Size * GScale, Color.FromArgb(GetAlpha(ESA.Color), (GameIsOver ? Com.ColorManipulation.GetGrayscaleColor(ESA.Color) : ESA.Color)), 0F, GAMatrix, TLDist, Com.PointD3D.Zero, false, Exposure, AntiAlias);
                }

                if (CharacterAffineMatrixList.Count > 0)
                {
                    List<Com.Matrix> CAMList = new List<Com.Matrix>(CharacterAffineMatrixList);

                    CAMList.Add(GAMatrix);

                    Com.Painting3D.PaintCuboid(GameBmp, Character.Center * GScale, Character.Size * GScale, Color.FromArgb(GetAlpha(Character.Color), (GameIsOver ? Com.ColorManipulation.GetGrayscaleColor(Character.Color) : Character.Color)), (float)GScale, CAMList, TLDist, IDirection, true, Exposure, AntiAlias);
                }
                else
                {
                    Com.Painting3D.PaintCuboid(GameBmp, Character.Center * GScale, Character.Size * GScale, Color.FromArgb(GetAlpha(Character.Color), (GameIsOver ? Com.ColorManipulation.GetGrayscaleColor(Character.Color) : Character.Color)), (float)GScale, GAMatrix, TLDist, IDirection, true, Exposure, AntiAlias);
                }

                //

                SizeF LinearGradientEdgeSize = new SizeF(Math.Max(1, GameBmpRect.Width / 16F), Math.Max(1, GameBmpRect.Height / 16F));

                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(-1, -1), new PointF(LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height), GameUIBackColor_DEC, Color.Transparent), new RectangleF(new PointF(0, 0), LinearGradientEdgeSize));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(-1, GameBmp.Height), new PointF(LinearGradientEdgeSize.Width, GameBmp.Height - LinearGradientEdgeSize.Height - 1), GameUIBackColor_DEC, Color.Transparent), new RectangleF(new PointF(0, GameBmp.Height - LinearGradientEdgeSize.Height), LinearGradientEdgeSize));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(GameBmp.Width, -1), new PointF(GameBmp.Width - LinearGradientEdgeSize.Width - 1, LinearGradientEdgeSize.Height), GameUIBackColor_DEC, Color.Transparent), new RectangleF(new PointF(GameBmp.Width - LinearGradientEdgeSize.Width, 0), LinearGradientEdgeSize));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(GameBmp.Width, GameBmp.Height), new PointF(GameBmp.Width - LinearGradientEdgeSize.Width - 1, GameBmp.Height - LinearGradientEdgeSize.Height - 1), GameUIBackColor_DEC, Color.Transparent), new RectangleF(new PointF(GameBmp.Width - LinearGradientEdgeSize.Width, GameBmp.Height - LinearGradientEdgeSize.Height), LinearGradientEdgeSize));

                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(-1, LinearGradientEdgeSize.Height), new PointF(LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height), Color.FromArgb(128, GameUIBackColor_DEC), Color.Transparent), new RectangleF(new PointF(0, LinearGradientEdgeSize.Height), new SizeF(LinearGradientEdgeSize.Width, GameBmp.Height - 2 * LinearGradientEdgeSize.Height)));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(GameBmp.Width, LinearGradientEdgeSize.Height), new PointF(GameBmp.Width - LinearGradientEdgeSize.Width - 1, LinearGradientEdgeSize.Height), Color.FromArgb(128, GameUIBackColor_DEC), Color.Transparent), new RectangleF(new PointF(GameBmp.Width - LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height), new SizeF(LinearGradientEdgeSize.Width, GameBmp.Height - 2 * LinearGradientEdgeSize.Height)));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(LinearGradientEdgeSize.Width, -1), new PointF(LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height), Color.FromArgb(128, GameUIBackColor_DEC), Color.Transparent), new RectangleF(new PointF(LinearGradientEdgeSize.Width, 0), new SizeF(GameBmp.Width - 2 * LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height)));
                GameBmpGrap.FillRectangle(new LinearGradientBrush(new PointF(LinearGradientEdgeSize.Width, GameBmp.Height), new PointF(LinearGradientEdgeSize.Width, GameBmp.Height - LinearGradientEdgeSize.Height - 1), Color.FromArgb(128, GameUIBackColor_DEC), Color.Transparent), new RectangleF(new PointF(LinearGradientEdgeSize.Width, GameBmp.Height - LinearGradientEdgeSize.Height), new SizeF(GameBmp.Width - 2 * LinearGradientEdgeSize.Width, LinearGradientEdgeSize.Height)));

                //

                if (GameIsOver)
                {
                    GameBmpGrap.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.White)), new Rectangle(new Point(0, 0), GameBmp.Size));

                    string StringText = "失败";
                    Color StringColor = Me.RecommendColors.Text.ToColor();

                    Font StringFont = Com.Text.GetSuitableFont(StringText, new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), new SizeF(GameBmp.Width * 0.8F, GameBmp.Height * 0.2F));
                    RectangleF StringRect = new RectangleF();
                    StringRect.Size = GameBmpGrap.MeasureString(StringText, StringFont);
                    StringRect.Location = new PointF((GameBmp.Width - StringRect.Width) / 2, (GameBmp.Height - StringRect.Height) / 2);

                    Color StringBkColor = Com.ColorManipulation.ShiftLightnessByHSL(StringColor, 0.5);
                    Rectangle StringBkRect = new Rectangle(new Point(0, (Int32)StringRect.Y), new Size(GameBmp.Width, Math.Max(1, (Int32)StringRect.Height)));

                    GraphicsPath Path_StringBk = new GraphicsPath();
                    Path_StringBk.AddRectangle(StringBkRect);
                    PathGradientBrush PGB_StringBk = new PathGradientBrush(Path_StringBk)
                    {
                        CenterColor = Color.FromArgb(192, StringBkColor),
                        SurroundColors = new Color[] { Color.Transparent },
                        FocusScales = new PointF(0F, 1F)
                    };
                    GameBmpGrap.FillPath(PGB_StringBk, Path_StringBk);
                    Path_StringBk.Dispose();
                    PGB_StringBk.Dispose();

                    Com.Painting2D.PaintTextWithShadow(GameBmp, StringText, StringFont, StringColor, StringColor, StringRect.Location, 0.02F, AntiAlias);
                }
            }
        }

        private void RepaintGameBmp()
        {
            //
            // 更新并重绘游戏位图。
            //

            if (Panel_Environment.Visible && (Panel_Environment.Width > 0 && Panel_Environment.Height > 0))
            {
                UpdateGameBmp();

                if (GameBmp != null)
                {
                    if (Panel_Environment.Width > GameBmp.Width)
                    {
                        Panel_Environment.CreateGraphics().FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size((Panel_Environment.Width - GameBmp.Width) / 2, Panel_Environment.Height)), new Rectangle(new Point(Panel_Environment.Width - (Panel_Environment.Width - GameBmp.Width) / 2, 0), new Size((Panel_Environment.Width - GameBmp.Width) / 2, Panel_Environment.Height)) });
                    }

                    if (Panel_Environment.Height > GameBmp.Height)
                    {
                        Panel_Environment.CreateGraphics().FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size(Panel_Environment.Width, (Panel_Environment.Height - GameBmp.Height) / 2)), new Rectangle(new Point(0, Panel_Environment.Height - (Panel_Environment.Height - GameBmp.Height) / 2), new Size(Panel_Environment.Width, (Panel_Environment.Height - GameBmp.Height) / 2)) });
                    }

                    Panel_Environment.CreateGraphics().DrawImage(GameBmp, GameBmpRect.Location);
                }
            }
        }

        private void Panel_Environment_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Environment 绘图。
            //

            if (GameBmp != null)
            {
                if (Panel_Environment.Width > GameBmp.Width)
                {
                    e.Graphics.FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size((Panel_Environment.Width - GameBmp.Width) / 2, Panel_Environment.Height)), new Rectangle(new Point(Panel_Environment.Width - (Panel_Environment.Width - GameBmp.Width) / 2, 0), new Size((Panel_Environment.Width - GameBmp.Width) / 2, Panel_Environment.Height)) });
                }

                if (Panel_Environment.Height > GameBmp.Height)
                {
                    e.Graphics.FillRectangles(new SolidBrush(GameUIBackColor_DEC), new Rectangle[] { new Rectangle(new Point(0, 0), new Size(Panel_Environment.Width, (Panel_Environment.Height - GameBmp.Height) / 2)), new Rectangle(new Point(0, Panel_Environment.Height - (Panel_Environment.Height - GameBmp.Height) / 2), new Size(Panel_Environment.Width, (Panel_Environment.Height - GameBmp.Height) / 2)) });
                }

                e.Graphics.DrawImage(GameBmp, GameBmpRect.Location);
            }
        }

        // 动画。

        private void AnimateShowEnterOngoingGame()
        {
            //
            // 以动画效果显示进入进行中的游戏。
            //

            if (PlatformList.Count > 0)
            {
                Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                {
                    double Pct_F = (frameId == frameCount ? 1 : 1 - Math.Pow(1 - (double)frameId / frameCount, 2));

                    GraphAffineMatrixList.Clear();
                    GraphAffineMatrixList.Add(Com.PointD3D.RotateXMatrix(90 * Math.PI / 180));
                    GraphAffineMatrixList.Add(Com.PointD3D.RotateYMatrix((NextDirection == Directions.Y_INC ? -45 * Pct_F : -90 + 45 * Pct_F) * Math.PI / 180));
                    GraphAffineMatrixList.Add(Com.PointD3D.RotateXMatrix(35 * Pct_F * Math.PI / 180));
                    GraphAffineMatrixList.Add(Com.PointD3D.OffsetMatrix(new Com.PointD3D(GameBmpRect.Width / 2, GameBmpRect.Height / 2, TrueLenDist * (2 - Pct_F))));

                    RepaintGameBmp();
                };

                Com.Animation.Show(Frame, 20, 15);

                GraphAffineMatrixList.Clear();
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowLastPlatformGenerate()
        {
            //
            // 以动画效果显示最后一个跳台的生成。
            //

            Cuboid LP = LastPlatform;

            if (!LP.IsEmpty)
            {
                Com.Animation.Frame FrameA = (frameId, frameCount, msPerFrame) =>
                {
                    double N = (frameId == frameCount ? 0 : 1 - Math.Pow((double)frameId / frameCount, 2));

                    LP.Color = Color.FromArgb(Math.Max(0, Math.Min((Int32)((double)frameId / frameCount * 255), 255)), LP.Color);
                    LP.Center.Z = -LP.Size.Z / 2 + LP.Size.Z * N;
                    LastPlatform = LP;

                    RepaintGameBmp();
                };

                Com.Animation.Show(FrameA, 10, 15);

                Com.Animation.Frame FrameB = (frameId, frameCount, msPerFrame) =>
                {
                    double N = (frameId == frameCount ? 0 : 0.2 * (1 - Math.Pow((frameId - frameCount * 0.4) / (frameCount * 0.6), 2)));

                    LP.Center.Z = -LP.Size.Z / 2 + LP.Size.Z * N;
                    LastPlatform = LP;

                    RepaintGameBmp();
                };

                Com.Animation.Show(FrameB, 5, 15);
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowCharacterGenerate()
        {
            //
            // 以动画效果显示角色的生成。
            //

            if (PlatformList.Count > 0)
            {
                Com.Animation.Frame FrameA = (frameId, frameCount, msPerFrame) =>
                {
                    double N = (frameId == frameCount ? 0 : 1 - Math.Pow((double)frameId / frameCount, 2));

                    Character.Color = Color.FromArgb(Math.Max(0, Math.Min((Int32)((double)frameId / frameCount * 255), 255)), Character.Color);
                    Character.Center.Z = Character.Size.Z / 2 + Character.Size.Z * N;

                    RepaintGameBmp();
                };

                Com.Animation.Show(FrameA, 10, 15);

                Com.Animation.Frame FrameB = (frameId, frameCount, msPerFrame) =>
                {
                    double N = (frameId == frameCount ? 0 : 0.2 * (1 - Math.Pow((frameId - frameCount * 0.4) / (frameCount * 0.6), 2)));

                    Character.Center.Z = Character.Size.Z / 2 + Character.Size.Z * N;

                    RepaintGameBmp();
                };

                Com.Animation.Show(FrameB, 5, 15);
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowCharacterJump(Com.PointD3D Src, Com.PointD3D Dest)
        {
            //
            // 以动画效果显示角色的跳跃。Src：角色位于起点时的中心坐标；Dest：角色位于终点时的中心坐标。
            //

            Cuboid LP = LastPlatform;

            if (!LP.IsEmpty)
            {
                Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                {
                    double Pct_F = (frameId == frameCount ? 1 : (double)frameId / frameCount);

                    Character.Center.XY = ((Src * (1 - Pct_F) + Dest * Pct_F)).XY;
                    Character.Center.Z = Src.Z + (Src - Dest).XY.Module / 4 - Math.Pow(((Src + Dest) / 2 - Character.Center).XY.Module, 2) / (Src - Dest).XY.Module;

                    double HalfDist = (NextDirection == Directions.X_INC ? Dest.X - Src.X : Dest.Y - Src.Y) / 2;
                    double DeltaS = (NextDirection == Directions.X_INC ? Character.Center.X - Src.X : Character.Center.Y - Src.Y);
                    double RotateAngle = (DeltaS < HalfDist ? Math.PI / 2 * Math.Pow(DeltaS / HalfDist, 2) : Math.PI - Math.PI / 2 * Math.Pow(2 - DeltaS / HalfDist, 2)) * (NextDirection == Directions.X_INC ? 1 : -1);
                    Com.Matrix RotateMatrix = (NextDirection == Directions.X_INC ? Com.PointD3D.RotateYMatrix(RotateAngle) : Com.PointD3D.RotateXMatrix(RotateAngle));

                    double GScale = GraphScale;
                    Com.Matrix OffsetMatrix1 = Com.PointD3D.OffsetMatrix(Character.Center * (-GScale));
                    Com.Matrix OffsetMatrix2 = Com.PointD3D.OffsetMatrix(Character.Center * GScale);

                    CharacterAffineMatrixList.Clear();
                    CharacterAffineMatrixList.Add(OffsetMatrix1);
                    CharacterAffineMatrixList.Add(RotateMatrix);
                    CharacterAffineMatrixList.Add(OffsetMatrix2);

                    RepaintGameBmp();
                };

                Com.Animation.Show(Frame, 20 + (Int32)Math.Round((Src - Dest).XY.Module * 4 / JumpVelocity), 15);

                CharacterAffineMatrixList.Clear();
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowCharacterDropOnPlatform(double Distance)
        {
            //
            // 以动画效果显示角色跳跃到跳台表面。Distance：角色跳跃的距离。
            //

            Cuboid CP = CurrentPlatform;

            if (!CP.IsEmpty)
            {
                Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                {
                    if (frameId < frameCount)
                    {
                        double Scale = Math.Sqrt(1 - Distance / MaxJumpDistance);

                        CP.Size.Z = Math.Max(1, PlatformHeight * Scale);
                        CP.Center.Z = -PlatformHeight + CP.Size.Z / 2;
                        CurrentPlatform = CP;

                        Character.Size.Z = Math.Max(1, CharacterSize.Z * Scale);
                        Character.Center.Z = CP.Center.Z + CP.Size.Z / 2 + Character.Size.Z / 2;
                    }
                    else
                    {
                        CP.Size.Z = PlatformHeight;
                        CP.Center.Z = -PlatformHeight / 2;
                        CurrentPlatform = CP;

                        Character.Size.Z = CharacterSize.Z;
                        Character.Center.Z = Character.Size.Z / 2;
                    }

                    RepaintGameBmp();
                };

                Com.Animation.Show(Frame, 4, 15);
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowCharacterMoveToCenter(Record OldRecord, Record NewRecord)
        {
            //
            // 以动画效果显示角色移动至屏幕中央。OldRecord，NewRecord：角色移动至屏幕中央之前与之后的记录。
            //

            if (!Character.IsEmpty)
            {
                List<Com.PointD> PlatformXY = new List<Com.PointD>(PlatformList.Count);

                for (int i = 0; i < PlatformList.Count; i++)
                {
                    PlatformXY.Add(PlatformList[i].Center.XY);
                }

                Com.PointD CharacterXY = Character.Center.XY;

                Com.Animation.Frame Frame = (frameId, frameCount, msPerFrame) =>
                {
                    double Pct_F;

                    if (frameId < frameCount * 0.5)
                    {
                        Pct_F = 0.5 * Math.Pow(frameId / (frameCount * 0.5), 2);
                    }
                    else if (frameId < frameCount)
                    {
                        Pct_F = 0.5 * (2 - Math.Pow((frameId - frameCount) / (frameCount * 0.5), 2));
                    }
                    else
                    {
                        Pct_F = 1;
                    }

                    for (int i = 0; i < PlatformList.Count; i++)
                    {
                        Cuboid P = PlatformList[i];

                        P.Center.XY = PlatformXY[i] - CharacterXY * Pct_F;

                        PlatformList[i] = P;
                    }

                    Character.Center.XY = CharacterXY - CharacterXY * Pct_F;

                    RepaintGameBmp();

                    if (OldRecord.Score != NewRecord.Score || OldRecord.Accuracy != NewRecord.Accuracy)
                    {
                        double Pct_Rec = (frameId == frameCount ? 1 : (double)frameId / frameCount);

                        ThisRecord.Accuracy = OldRecord.Accuracy * (1 - Pct_Rec) + NewRecord.Accuracy * Pct_Rec;

                        RepaintCurBmp(true);
                    }
                };

                Com.Animation.Show(Frame, 20 + (Int32)Math.Round(Character.Center.XY.Module * 4 / JumpVelocity), 15);
            }
            else
            {
                RepaintGameBmp();
            }
        }

        private void AnimateShowCharacterMoveToCenter()
        {
            //
            // 以动画效果显示角色移动至屏幕中央。
            //

            if (!Character.IsEmpty)
            {
                AnimateShowCharacterMoveToCenter(ThisRecord, ThisRecord);
            }
            else
            {
                RepaintGameBmp();
            }
        }

        // 逻辑操作。

        private enum Directions { NULL = -1, X_INC, Y_INC, COUNT } // 方向枚举。
        private Directions NextDirection = Directions.NULL; // 下一步的方向。

        private void AppendPlatform()
        {
            //
            // 向列表添加一个跳台。
            //

            double Diff = Difficulty;

            double SzMin = PlatformMinWidth + (PlatformMaxWidth - PlatformMinWidth) / 2 * (1 - Diff), SzMax = PlatformMinWidth + (PlatformMaxWidth - PlatformMinWidth) * (1 - Diff);
            double DistMin = PlatformMinDist + (PlatformMaxDist + (PlatformMaxWidth - SzMax) - PlatformMinDist) / 2 * (1 - Diff), DistMax = PlatformMinDist + (PlatformMaxDist + (PlatformMaxWidth - SzMax) - PlatformMinDist) / 2 * (1 + Diff);

            Color Cr = Com.ColorManipulation.GetRandomColorX().AtLightness_HSL(60).ToColor();

            if (PlatformList.Count == 0)
            {
                PlatformList.Add(new Cuboid
                {
                    Center = new Com.PointD3D(0, 0, -PlatformHeight / 2),
                    Size = new Com.PointD3D((SzMin + SzMax) / 2, (SzMin + SzMax) / 2, PlatformHeight),
                    Color = Cr
                });
            }
            else
            {
                NextDirection = (Directions)Com.Statistics.RandomInteger((Int32)Directions.COUNT);

                Cuboid LP = LastPlatform;

                if (!LP.IsEmpty)
                {
                    Com.PointD3D Sz, Cen;

                    if (PlatformList.Count == 1)
                    {
                        Sz = new Com.PointD3D((SzMin + SzMax) / 2, (SzMin + SzMax) / 2, PlatformHeight);
                        Cen = (NextDirection == Directions.X_INC ? new Com.PointD3D(LP.Center.X + (LP.Size.X + Sz.X) / 2 + (DistMin + DistMax) / 2, LP.Center.Y, LP.Center.Z) : new Com.PointD3D(LP.Center.X, LP.Center.Y + (LP.Size.Y + Sz.Y) / 2 + (DistMin + DistMax) / 2, LP.Center.Z));
                    }
                    else
                    {
                        Sz = new Com.PointD3D(Com.Statistics.RandomDouble(SzMin, SzMax), Com.Statistics.RandomDouble(SzMin, SzMax), PlatformHeight);
                        Cen = (NextDirection == Directions.X_INC ? new Com.PointD3D(LP.Center.X + (LP.Size.X + Sz.X) / 2 + Com.Statistics.RandomDouble(DistMin, DistMax), LP.Center.Y, LP.Center.Z) : new Com.PointD3D(LP.Center.X, LP.Center.Y + (LP.Size.Y + Sz.Y) / 2 + Com.Statistics.RandomDouble(DistMin, DistMax), LP.Center.Z));
                    }

                    PlatformList.Add(new Cuboid
                    {
                        Center = Cen,
                        Size = Sz,
                        Color = Cr
                    });
                }
            }
        }

        private void ShrinkPlatformList()
        {
            //
            // 收缩跳台列表。
            //

            while (PlatformList.Count > 2)
            {
                if (!PlatformList[0].Painted)
                {
                    PlatformList.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }

        private void StartNewGame()
        {
            //
            // 开始新游戏。
            //

            PlatformList.Clear();

            Character = Cuboid.Empty;

            //

            AppendPlatform();

            AnimateShowLastPlatformGenerate();

            //

            Character.Size = CharacterSize;
            Character.Center = new Com.PointD3D(0, 0, Character.Size.Z / 2);
            Character.Color = Com.ColorManipulation.GetRandomColorX().AtLightness_HSL(15).ToColor();

            AnimateShowCharacterGenerate();

            //

            AppendPlatform();

            AnimateShowLastPlatformGenerate();
        }

        private DateTime PressDownTime = DateTime.Now; // 按下的时刻。

        private void PressDown()
        {
            //
            // 按下。
            //

            PressDownTime = DateTime.Now;

            Timer_PressDownAnimation.Enabled = true;
        }

        private void PressUp()
        {
            //
            // 释放。
            //

            Timer_PressDownAnimation.Enabled = false;

            double Seconds = Math.Min(MaxPressDownSeconds, (DateTime.Now - PressDownTime).TotalSeconds);

            Cuboid CP = CurrentPlatform;
            Cuboid LP = LastPlatform;

            if (!CP.IsEmpty && !LP.IsEmpty)
            {
                CP.Size.Z = PlatformHeight;
                CP.Center.Z = -PlatformHeight / 2;
                CurrentPlatform = CP;

                Character.Size.Z = CharacterSize.Z;
                Character.Center.Z = Character.Size.Z / 2;

                RepaintGameBmp();

                if (Seconds >= MinPressDownSeconds)
                {
                    Com.PointD3D Src = Character.Center;
                    Com.PointD3D Dest = (NextDirection == Directions.X_INC ? new Com.PointD3D(Character.Center.X + Seconds * JumpVelocity, LP.Center.Y, Character.Center.Z) : new Com.PointD3D(LP.Center.X, Character.Center.Y + Seconds * JumpVelocity, Character.Center.Z));

                    AnimateShowCharacterJump(Src, Dest);

                    CP = CurrentPlatform;

                    if (!CP.IsEmpty)
                    {
                        AnimateShowCharacterDropOnPlatform((Src - Dest).XY.Module);

                        CP = CurrentPlatform;
                        LP = LastPlatform;

                        if (CP.Equals(LP))
                        {
                            Record OldRecord = ThisRecord, NewRecord = ThisRecord;

                            ThisRecord.Score += 1;
                            ThisRecord.PlatformCount++;

                            double Accuracy = (NextDirection == Directions.X_INC ? 1 - Math.Abs((Character.Center.X - LP.Center.X) / (LP.Size.X / 2)) : 1 - Math.Abs((Character.Center.Y - LP.Center.Y) / (LP.Size.Y / 2)));

                            ThisRecord.Accuracy = (ThisRecord.Accuracy * (ThisRecord.PlatformCount - 1) + Accuracy) / ThisRecord.PlatformCount;

                            Com.PointD ESASize = ExtraScoreAreaSize;

                            if (Com.Geometry.PointIsVisibleInRectangle(Character.Center.XY, new RectangleF((LP.Center.XY - ESASize / 2).ToPointF(), ESASize.ToSizeF())))
                            {
                                if (ThisRecord.LastExtraScore == 0)
                                {
                                    ThisRecord.LastExtraScore = 1;
                                }
                                else
                                {
                                    ThisRecord.LastExtraScore += 2;
                                }

                                ThisRecord.Score += ThisRecord.LastExtraScore;
                            }
                            else
                            {
                                ThisRecord.LastExtraScore = 0;
                            }

                            NewRecord.Accuracy = ThisRecord.Accuracy;

                            AnimateShowCharacterMoveToCenter(OldRecord, NewRecord);

                            CP = CurrentPlatform;
                            LP = LastPlatform;

                            ShrinkPlatformList();

                            AppendPlatform();

                            AnimateShowLastPlatformGenerate();

                            RepaintCurBmp(false);
                        }
                        else
                        {
                            AnimateShowCharacterMoveToCenter();
                        }
                    }
                }
            }
            else
            {
                RepaintGameBmp();
            }

            //

            Judgement();
        }

        private void Timer_PressDownAnimation_Tick(object sender, EventArgs e)
        {
            //
            // Timer_PressDownAnimation。
            //

            Cuboid CP = CurrentPlatform;

            if (!CP.IsEmpty)
            {
                double MPDSeconds = MaxPressDownSeconds;

                double Seconds = Math.Min(MPDSeconds, (DateTime.Now - PressDownTime).TotalSeconds);

                if (Seconds >= MinPressDownSeconds)
                {
                    CP.Size.Z = Math.Max(1, PlatformHeight * Math.Pow((MPDSeconds - Seconds) / MPDSeconds, 2));
                    CP.Center.Z = -PlatformHeight + CP.Size.Z / 2;
                    CurrentPlatform = CP;

                    Character.Size.Z = Math.Max(1, CharacterSize.Z * Math.Pow((MPDSeconds - Seconds) / MPDSeconds, 2));
                    Character.Center.Z = CP.Center.Z + CP.Size.Z / 2 + Character.Size.Z / 2;

                    RepaintGameBmp();
                }
            }
        }

        #endregion

        #region 中断管理

        // 判定。

        private void Judgement()
        {
            //
            // 失败判定。
            //

            if (!GameIsOver)
            {
                if (CurrentPlatform.IsEmpty)
                {
                    GameIsOver = true;

                    RepaintGameBmp();

                    ThisGameTime = (DateTime.Now - GameStartingTime);
                    TotalGameTime += ThisGameTime;

                    SaveUserData();

                    EraseLastGame();
                }
            }

            //

            RepaintCurBmp();
        }

        // 中断。

        private enum InterruptActions { NULL = -1, StartNew, Continue, Restart, ExitGame, CloseApp, COUNT }; // 中断动作枚举。

        private void Interrupt(InterruptActions IA)
        {
            //
            // 中断。
            //

            switch (IA)
            {
                case InterruptActions.StartNew: // 开始新游戏。
                    {
                        EraseLastGame();

                        //

                        EnterGameUI();

                        //

                        StartNewGame();

                        Judgement();
                    }
                    break;

                case InterruptActions.Continue: // 继续上次的游戏。
                    {
                        EnterGameUI();

                        //

                        ThisRecord = Record_Last;

                        NextDirection = Record_Last.NextDirection;

                        foreach (Cuboid Cub in PlatformList_Last)
                        {
                            PlatformList.Add(Cub);
                        }

                        Character.Size = CharacterSize;
                        Character.Center = new Com.PointD3D(0, 0, Character.Size.Z / 2);
                        Character.Color = Character_Last.Color;

                        AnimateShowEnterOngoingGame();

                        Judgement();
                    }
                    break;

                case InterruptActions.Restart: // 重新开始。
                    {
                        EraseLastGame();

                        //

                        if (!GameIsOver)
                        {
                            TotalGameTime += (DateTime.Now - GameStartingTime);
                        }

                        SaveUserData();

                        GameStartingTime = DateTime.Now;

                        //

                        GameIsOver = false;

                        ThisRecord = new Record();

                        RepaintCurBmp();

                        StartNewGame();

                        Judgement();

                        //

                        Panel_Environment.Focus();
                    }
                    break;

                case InterruptActions.ExitGame: // 退出游戏。
                    {
                        if (!GameIsOver)
                        {
                            ThisGameTime = (DateTime.Now - GameStartingTime);
                            TotalGameTime += ThisGameTime;
                        }

                        SaveUserData();

                        //

                        Panel_Environment.Focus();

                        //

                        if (!GameIsOver && ThisRecord.Score > 0)
                        {
                            SaveLastGame();
                        }

                        ExitGameUI();
                    }
                    break;

                case InterruptActions.CloseApp: // 关闭程序。
                    {
                        if (!GameIsOver)
                        {
                            ThisGameTime = (DateTime.Now - GameStartingTime);
                            TotalGameTime += ThisGameTime;
                        }

                        SaveUserData();

                        //

                        if (!GameIsOver && ThisRecord.Score > 0)
                        {
                            SaveLastGame();
                        }
                    }
                    break;
            }
        }

        // 中断按钮。

        private static class InterruptImages // 包含表示中断的图像的静态类。
        {
            private static readonly Size _Size = new Size(25, 25);

            private static Bitmap _Restart = null;
            private static Bitmap _ExitGame = null;

            //

            public static Bitmap Restart => _Restart; // 重新开始。
            public static Bitmap ExitGame => _ExitGame; // 退出游戏。

            //

            public static void Update(Color color) // 使用指定的颜色更新所有图像。
            {
                _Restart = new Bitmap(_Size.Width, _Size.Height);

                using (Graphics Grap = Graphics.FromImage(_Restart))
                {
                    Grap.SmoothingMode = SmoothingMode.AntiAlias;

                    Grap.DrawArc(new Pen(color, 2F), new Rectangle(new Point(5, 5), new Size(15, 15)), -150F, 300F);
                    Grap.DrawLines(new Pen(color, 2F), new Point[] { new Point(5, 5), new Point(5, 10), new Point(10, 10) });
                }

                //

                _ExitGame = new Bitmap(_Size.Width, _Size.Height);

                using (Graphics Grap = Graphics.FromImage(_ExitGame))
                {
                    Grap.SmoothingMode = SmoothingMode.AntiAlias;

                    Grap.DrawLine(new Pen(color, 2F), new Point(5, 5), new Point(19, 19));
                    Grap.DrawLine(new Pen(color, 2F), new Point(19, 5), new Point(5, 19));
                }
            }
        }

        private void Label_StartNewGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_StartNewGame。
            //

            Interrupt(InterruptActions.StartNew);
        }

        private void Label_ContinueLastGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_ContinueLastGame。
            //

            Interrupt(InterruptActions.Continue);
        }

        private void PictureBox_Restart_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 PictureBox_Restart。
            //

            ToolTip_InterruptPrompt.RemoveAll();

            ToolTip_InterruptPrompt.SetToolTip(PictureBox_Restart, "重新开始");
        }

        private void PictureBox_Restart_Click(object sender, EventArgs e)
        {
            //
            // 单击 PictureBox_Restart。
            //

            Interrupt(InterruptActions.Restart);
        }

        private void PictureBox_ExitGame_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 PictureBox_ExitGame。
            //

            ToolTip_InterruptPrompt.RemoveAll();

            ToolTip_InterruptPrompt.SetToolTip(PictureBox_ExitGame, (!GameIsOver && ThisRecord.Score > 0 ? "保存并退出" : "退出"));
        }

        private void PictureBox_ExitGame_Click(object sender, EventArgs e)
        {
            //
            // 单击 PictureBox_ExitGame。
            //

            Interrupt(InterruptActions.ExitGame);
        }

        #endregion

        #region UI 切换

        private bool GameUINow = false; // 当前 UI 是否为游戏 UI。

        private void EnterGameUI()
        {
            //
            // 进入游戏 UI。
            //

            GameUINow = true;

            //

            PlatformList.Clear();

            Character = Cuboid.Empty;

            //

            GameIsOver = false;

            GameStartingTime = DateTime.Now;

            ThisRecord = new Record();

            //

            Panel_FunctionArea.Visible = false;
            Panel_GameUI.Visible = true;

            //

            Panel_Environment.Focus();

            //

            while (GameBmpSize.Width > Screen.PrimaryScreen.WorkingArea.Width || Me.CaptionBarHeight + Panel_Current.Height + GameBmpSize.Height > Screen.PrimaryScreen.WorkingArea.Height)
            {
                GameBmpSize = new Size(GameBmpSize.Width * 9 / 10, GameBmpSize.Height * 9 / 10);
            }

            Rectangle NewBounds = new Rectangle();
            NewBounds.Size = new Size(GameBmpSize.Width, Me.CaptionBarHeight + Panel_Current.Height + GameBmpSize.Height);
            NewBounds.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - NewBounds.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - NewBounds.Height) / 2);
            Me.Bounds = NewBounds;

            Int32 Sz = Math.Min(Panel_Environment.Width, Panel_Environment.Height);

            GameBmpSize = new Size(Sz, Sz);

            GameBmpRect.Location = new Point((Panel_Environment.Width - GameBmpSize.Width) / 2, (Panel_Environment.Height - GameBmpSize.Height) / 2);
            GameBmpRect.Size = GameBmpSize;

            //

            RepaintCurBmp();

            RepaintGameBmp();
        }

        private void ExitGameUI()
        {
            //
            // 退出游戏 UI。
            //

            GameUINow = false;

            //

            Panel_FunctionArea.Visible = true;
            Panel_GameUI.Visible = false;

            //

            Rectangle NewBounds = new Rectangle();
            NewBounds.Size = new Size(FormClientInitialSize.Width, Me.CaptionBarHeight + FormClientInitialSize.Height);
            NewBounds.Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - NewBounds.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - NewBounds.Height) / 2);
            Me.Bounds = NewBounds;

            //

            FunctionAreaTab = FunctionAreaTabs.Start;
        }

        #endregion

        #region 游戏 UI 交互

        private void Panel_Environment_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Panel_Environment。
            //

            if (Me.IsActive)
            {
                Panel_Environment.Focus();
            }
        }

        private void Panel_Environment_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Panel_Environment。
            //

            if (!GameIsOver && e.Button == MouseButtons.Left)
            {
                PressDown();
            }
        }

        private void Panel_Environment_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Panel_Environment。
            //

            if (!GameIsOver && e.Button == MouseButtons.Left)
            {
                PressUp();
            }
        }

        private void Panel_Environment_KeyDown(object sender, KeyEventArgs e)
        {
            //
            // 在 Panel_Environment 按下键。
            //

            switch (e.KeyCode)
            {
                case Keys.Home: Interrupt(InterruptActions.Restart); break;
                case Keys.End:
                case Keys.Escape: Interrupt(InterruptActions.ExitGame); break;
            }
        }

        #endregion

        #region 鼠标滚轮功能

        private void Panel_FunctionAreaOptionsBar_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 Panel_FunctionAreaOptionsBar 滚动。
            //

            if (e.Delta < 0 && (Int32)FunctionAreaTab < (Int32)FunctionAreaTabs.COUNT - 1)
            {
                FunctionAreaTab++;
            }
            else if (e.Delta > 0 && (Int32)FunctionAreaTab > 0)
            {
                FunctionAreaTab--;
            }
        }

        private void Panel_Environment_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 Panel_Environment 滚动。
            //

            Rectangle NewBounds = Me.Bounds;

            Size GBmpSize = GameBmpRect.Size;

            if (GBmpSize.Width <= GBmpSize.Height)
            {
                if (e.Delta > 0)
                {
                    NewBounds.Location = new Point(NewBounds.X - NewBounds.Width / 20, NewBounds.Y - NewBounds.Width / 20 * GBmpSize.Height / GBmpSize.Width);
                    NewBounds.Size = new Size(NewBounds.Width + NewBounds.Width / 20 * 2, NewBounds.Height + NewBounds.Width / 20 * GBmpSize.Height / GBmpSize.Width * 2);
                }
                else if (e.Delta < 0)
                {
                    NewBounds.Location = new Point(NewBounds.X + NewBounds.Width / 20, NewBounds.Y + NewBounds.Width / 20 * GBmpSize.Height / GBmpSize.Width);
                    NewBounds.Size = new Size(NewBounds.Width - NewBounds.Width / 20 * 2, NewBounds.Height - NewBounds.Width / 20 * GBmpSize.Height / GBmpSize.Width * 2);
                }
            }
            else
            {
                if (e.Delta > 0)
                {
                    NewBounds.Location = new Point(NewBounds.X - NewBounds.Height / 20 * GBmpSize.Width / GBmpSize.Height, NewBounds.Y - NewBounds.Height / 20);
                    NewBounds.Size = new Size(NewBounds.Width + NewBounds.Height / 20 * GBmpSize.Width / GBmpSize.Height * 2, NewBounds.Height + NewBounds.Height / 20 * 2);
                }
                else if (e.Delta < 0)
                {
                    NewBounds.Location = new Point(NewBounds.X + NewBounds.Height / 20 * GBmpSize.Width / GBmpSize.Height, NewBounds.Y + NewBounds.Height / 20);
                    NewBounds.Size = new Size(NewBounds.Width - NewBounds.Height / 20 * GBmpSize.Width / GBmpSize.Height * 2, NewBounds.Height - NewBounds.Height / 20 * 2);
                }
            }

            NewBounds.Location = new Point(Math.Max(0, Math.Min(Screen.PrimaryScreen.WorkingArea.Width - NewBounds.Width, NewBounds.X)), Math.Max(0, Math.Min(Screen.PrimaryScreen.WorkingArea.Height - NewBounds.Height, NewBounds.Y)));

            Me.Bounds = NewBounds;
        }

        #endregion

        #region 计分栏

        private Bitmap CurBmp; // 计分栏位图。

        private void UpdateCurBmp(bool ShowLastAddedScore)
        {
            //
            // 更新计分栏位图。ShowLastAddedScore：是否显示最后的新增分数。
            //

            if (CurBmp != null)
            {
                CurBmp.Dispose();
            }

            CurBmp = new Bitmap(Math.Max(1, Panel_Current.Width), Math.Max(1, Panel_Current.Height));

            using (Graphics CurBmpGrap = Graphics.FromImage(CurBmp))
            {
                if (AntiAlias)
                {
                    CurBmpGrap.SmoothingMode = SmoothingMode.AntiAlias;
                    CurBmpGrap.TextRenderingHint = TextRenderingHint.AntiAlias;
                }

                CurBmpGrap.Clear(GameUIBackColor_DEC);

                //

                Rectangle Rect_Total = new Rectangle(new Point(0, 0), new Size(Math.Max(1, Panel_Current.Width), Math.Max(1, Panel_Current.Height)));
                Rectangle Rect_Current = new Rectangle(Rect_Total.Location, new Size((Int32)Math.Max(2, Math.Min(1, ThisRecord.Accuracy) * Rect_Total.Width), Rect_Total.Height));

                Color RectCr_Total = Me.RecommendColors.Background.ToColor(), RectCr_Current = Me.RecommendColors.Border.ToColor();

                GraphicsPath Path_Total = new GraphicsPath();
                Path_Total.AddRectangle(Rect_Total);
                PathGradientBrush PGB_Total = new PathGradientBrush(Path_Total)
                {
                    CenterColor = RectCr_Total,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr_Total, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                CurBmpGrap.FillPath(PGB_Total, Path_Total);
                Path_Total.Dispose();
                PGB_Total.Dispose();

                GraphicsPath Path_Current = new GraphicsPath();
                Path_Current.AddRectangle(Rect_Current);
                PathGradientBrush PGB_Current = new PathGradientBrush(Path_Current)
                {
                    CenterColor = RectCr_Current,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr_Current, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                CurBmpGrap.FillPath(PGB_Current, Path_Current);
                Path_Current.Dispose();
                PGB_Current.Dispose();

                //

                SizeF RegionSize_L = new SizeF(), RegionSize_R = new SizeF();
                RectangleF RegionRect = new RectangleF();

                string StringText_Score = (ThisRecord.Score > 0 && ShowLastAddedScore ? Math.Max(0, ThisRecord.Score - (1 + ThisRecord.LastExtraScore)) + "+" + Math.Max(0, 1 + ThisRecord.LastExtraScore) : Math.Max(0, ThisRecord.Score).ToString());
                Color StringColor_Score = Me.RecommendColors.Text_INC.ToColor();
                Font StringFont_Score = new Font("微软雅黑", 24F, FontStyle.Regular, GraphicsUnit.Point, 134);
                RectangleF StringRect_Score = new RectangleF();
                StringRect_Score.Size = CurBmpGrap.MeasureString(StringText_Score, StringFont_Score);

                string StringText_PlatformCount = "跳台数: ", StringText_PlatformCount_Val = Math.Max(0, ThisRecord.PlatformCount).ToString();
                Color StringColor_PlatformCount = Me.RecommendColors.Text.ToColor(), StringColor_PlatformCount_Val = Me.RecommendColors.Text_INC.ToColor();
                Font StringFont_PlatformCount = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134), StringFont_PlatformCount_Val = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
                RectangleF StringRect_PlatformCount = new RectangleF(), StringRect_PlatformCount_Val = new RectangleF();
                StringRect_PlatformCount.Size = CurBmpGrap.MeasureString(StringText_PlatformCount, StringFont_PlatformCount);
                StringRect_PlatformCount_Val.Size = CurBmpGrap.MeasureString(StringText_PlatformCount_Val, StringFont_PlatformCount_Val);

                string StringText_Accuracy = "准确度: ", StringText_Accuracy_Val = Math.Max(0, ThisRecord.Accuracy * 100).ToString("N1") + "%";
                Color StringColor_Accuracy = Me.RecommendColors.Text.ToColor(), StringColor_Accuracy_Val = Me.RecommendColors.Text_INC.ToColor();
                Font StringFont_Accuracy = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134), StringFont_Accuracy_Val = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
                RectangleF StringRect_Accuracy = new RectangleF(), StringRect_Accuracy_Val = new RectangleF();
                StringRect_Accuracy.Size = CurBmpGrap.MeasureString(StringText_Accuracy, StringFont_Accuracy);
                StringRect_Accuracy_Val.Size = CurBmpGrap.MeasureString(StringText_Accuracy_Val, StringFont_Accuracy_Val);

                RegionSize_L = StringRect_Score.Size;
                RegionSize_R = new SizeF(Math.Max(StringRect_PlatformCount.Width + StringRect_PlatformCount_Val.Width, StringRect_Accuracy.Width + StringRect_Accuracy_Val.Width), 0);

                RegionRect.Size = new SizeF(Math.Max(RegionSize_L.Width + RegionSize_R.Width, Math.Min(GameBmpRect.Width, Panel_Interrupt.Left - GameBmpRect.X)), Panel_Current.Height);
                RegionRect.Location = new PointF(Math.Max(0, Math.Min(GameBmpRect.X + (GameBmpRect.Width - RegionRect.Width) / 2, Panel_Interrupt.Left - RegionRect.Width)), 0);

                StringRect_Score.Location = new PointF(RegionRect.X, (RegionRect.Height - StringRect_Score.Height) / 2);

                Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Score, StringFont_Score, StringColor_Score, StringColor_Score, StringRect_Score.Location, 0.05F, AntiAlias);

                StringRect_PlatformCount_Val.Location = new PointF(RegionRect.Right - StringRect_PlatformCount_Val.Width, (RegionRect.Height / 2 - StringRect_PlatformCount_Val.Height) / 2);
                StringRect_PlatformCount.Location = new PointF(StringRect_PlatformCount_Val.X - StringRect_PlatformCount.Width, (RegionRect.Height / 2 - StringRect_PlatformCount.Height) / 2);

                Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_PlatformCount, StringFont_PlatformCount, StringColor_PlatformCount, StringColor_PlatformCount, StringRect_PlatformCount.Location, 0.1F, AntiAlias);
                Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_PlatformCount_Val, StringFont_PlatformCount_Val, StringColor_PlatformCount_Val, StringColor_PlatformCount_Val, StringRect_PlatformCount_Val.Location, 0.1F, AntiAlias);

                StringRect_Accuracy_Val.Location = new PointF(RegionRect.Right - StringRect_Accuracy_Val.Width, RegionRect.Height / 2 + (RegionRect.Height / 2 - StringRect_Accuracy_Val.Height) / 2);
                StringRect_Accuracy.Location = new PointF(StringRect_Accuracy_Val.X - StringRect_Accuracy.Width, RegionRect.Height / 2 + (RegionRect.Height / 2 - StringRect_Accuracy.Height) / 2);

                Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Accuracy, StringFont_Accuracy, StringColor_Accuracy, StringColor_Accuracy, StringRect_Accuracy.Location, 0.1F, AntiAlias);
                Com.Painting2D.PaintTextWithShadow(CurBmp, StringText_Accuracy_Val, StringFont_Accuracy_Val, StringColor_Accuracy_Val, StringColor_Accuracy_Val, StringRect_Accuracy_Val.Location, 0.1F, AntiAlias);
            }
        }

        private void UpdateCurBmp()
        {
            //
            // 更新计分栏位图。
            //

            UpdateCurBmp(false);
        }

        private void RepaintCurBmp(bool ShowLastAddedScore)
        {
            //
            // 更新并重绘计分栏位图。ShowLastAddedScore：是否显示最后的新增分数。
            //

            UpdateCurBmp(ShowLastAddedScore);

            if (CurBmp != null)
            {
                Panel_Current.CreateGraphics().DrawImage(CurBmp, new Point(0, 0));

                foreach (object Obj in Panel_Current.Controls)
                {
                    ((Control)Obj).Refresh();
                }
            }
        }

        private void RepaintCurBmp()
        {
            //
            // 更新并重绘计分栏位图。
            //

            RepaintCurBmp(false);
        }

        private void Panel_Current_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_Current 绘图。
            //

            UpdateCurBmp();

            if (CurBmp != null)
            {
                e.Graphics.DrawImage(CurBmp, new Point(0, 0));
            }
        }

        #endregion

        #region 功能区

        private enum FunctionAreaTabs { NULL = -1, Start, Record, Options, About, COUNT } // 功能区选项卡枚举。

        private FunctionAreaTabs _FunctionAreaTab = FunctionAreaTabs.NULL; // 当前打开的功能区选项卡。
        private FunctionAreaTabs FunctionAreaTab
        {
            get
            {
                return _FunctionAreaTab;
            }

            set
            {
                _FunctionAreaTab = value;

                Color TabBtnCr_Fr_Seld = Me.RecommendColors.Main_INC.ToColor(), TabBtnCr_Fr_Uns = Color.White;
                Color TabBtnCr_Bk_Seld = Color.Transparent, TabBtnCr_Bk_Uns = Color.Transparent;
                Font TabBtnFt_Seld = new Font("微软雅黑", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 134), TabBtnFt_Uns = new Font("微软雅黑", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 134);

                Label_Tab_Start.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Start.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Start.Font = (_FunctionAreaTab == FunctionAreaTabs.Start ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_Record.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Record.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Record.Font = (_FunctionAreaTab == FunctionAreaTabs.Record ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_Options.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_Options.BackColor = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_Options.Font = (_FunctionAreaTab == FunctionAreaTabs.Options ? TabBtnFt_Seld : TabBtnFt_Uns);

                Label_Tab_About.ForeColor = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnCr_Fr_Seld : TabBtnCr_Fr_Uns);
                Label_Tab_About.BackColor = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnCr_Bk_Seld : TabBtnCr_Bk_Uns);
                Label_Tab_About.Font = (_FunctionAreaTab == FunctionAreaTabs.About ? TabBtnFt_Seld : TabBtnFt_Uns);

                switch (_FunctionAreaTab)
                {
                    case FunctionAreaTabs.Start:
                        {
                            if (Record_Last.Score > 0)
                            {
                                Label_ContinueLastGame.Visible = true;

                                Label_ContinueLastGame.Focus();
                            }
                            else
                            {
                                Label_ContinueLastGame.Visible = false;

                                Label_StartNewGame.Focus();
                            }
                        }
                        break;

                    case FunctionAreaTabs.Record:
                        {
                            if (BestRecord.Score == 0)
                            {
                                Label_ThisRecordVal_Score.Text = "无记录";
                                Label_ThisRecordVal_PlatformCountAndAccuracy.Text = "跳台数: 无 / 准确度: 无";
                                Label_BestRecordVal_Score.Text = "无记录";
                                Label_BestRecordVal_PlatformCountAndAccuracy.Text = "跳台数: 无 / 准确度: 无";
                            }
                            else
                            {
                                Label_ThisRecordVal_Score.Text = ThisRecord.Score.ToString();
                                Label_ThisRecordVal_PlatformCountAndAccuracy.Text = "跳台数: " + ThisRecord.PlatformCount + " / 准确度: " + (ThisRecord.Accuracy * 100).ToString("N1") + "%";
                                Label_BestRecordVal_Score.Text = BestRecord.Score.ToString();
                                Label_BestRecordVal_PlatformCountAndAccuracy.Text = "跳台数: " + BestRecord.PlatformCount + " / 准确度: " + (BestRecord.Accuracy * 100).ToString("N1") + "%";
                            }

                            Label_ThisTimeVal.Text = Com.Text.GetTimeStringFromTimeSpan(ThisGameTime);
                            Label_TotalTimeVal.Text = Com.Text.GetTimeStringFromTimeSpan(TotalGameTime);
                        }
                        break;

                    case FunctionAreaTabs.Options:
                        {

                        }
                        break;

                    case FunctionAreaTabs.About:
                        {

                        }
                        break;
                }

                Timer_EnterPrompt.Enabled = (_FunctionAreaTab == FunctionAreaTabs.Start);

                if (Panel_FunctionAreaTab.AutoScroll)
                {
                    // Panel 的 AutoScroll 功能似乎存在 bug，下面的代码可以规避某些显示问题

                    Panel_FunctionAreaTab.AutoScroll = false;

                    foreach (object Obj in Panel_FunctionAreaTab.Controls)
                    {
                        if (Obj is Panel)
                        {
                            Panel Pnl = Obj as Panel;

                            Pnl.Location = new Point(0, 0);
                        }
                    }

                    Panel_FunctionAreaTab.AutoScroll = true;
                }

                Panel_Tab_Start.Visible = (_FunctionAreaTab == FunctionAreaTabs.Start);
                Panel_Tab_Record.Visible = (_FunctionAreaTab == FunctionAreaTabs.Record);
                Panel_Tab_Options.Visible = (_FunctionAreaTab == FunctionAreaTabs.Options);
                Panel_Tab_About.Visible = (_FunctionAreaTab == FunctionAreaTabs.About);
            }
        }

        private void Label_Tab_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Label_Tab。
            //

            Panel_FunctionAreaOptionsBar.Refresh();
        }

        private void Label_Tab_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Label_Tab。
            //

            Panel_FunctionAreaOptionsBar.Refresh();
        }

        private void Label_Tab_Start_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Start。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Start)
                {
                    FunctionAreaTab = FunctionAreaTabs.Start;
                }
            }
        }

        private void Label_Tab_Record_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Record。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Record)
                {
                    FunctionAreaTab = FunctionAreaTabs.Record;
                }
            }
        }

        private void Label_Tab_Options_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_Options。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.Options)
                {
                    FunctionAreaTab = FunctionAreaTabs.Options;
                }
            }
        }

        private void Label_Tab_About_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Label_Tab_About。
            //

            if (e.Button == MouseButtons.Left)
            {
                if (FunctionAreaTab != FunctionAreaTabs.About)
                {
                    FunctionAreaTab = FunctionAreaTabs.About;
                }
            }
        }

        #endregion

        #region "开始"区域

        private const Int32 EnterGameButtonHeight_Min = 30, EnterGameButtonHeight_Max = 50; // 进入游戏按钮高度的取值范围。

        private Color EnterGameBackColor_INC = Color.Empty; // Panel_EnterGameSelection 绘图使用的颜色（深色）。
        private Color EnterGameBackColor_DEC => Panel_FunctionArea.BackColor; // Panel_EnterGameSelection 绘图使用的颜色（浅色）。

        private void Panel_EnterGameSelection_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_EnterGameSelection 绘图。
            //

            Rectangle Rect_StartNew = new Rectangle(Label_StartNewGame.Location, Label_StartNewGame.Size);

            Color Cr_StartNew = Com.ColorManipulation.BlendByRGB(EnterGameBackColor_INC, EnterGameBackColor_DEC, Math.Sqrt((double)(Label_StartNewGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min)));

            GraphicsPath Path_StartNew = new GraphicsPath();
            Path_StartNew.AddRectangle(Rect_StartNew);
            PathGradientBrush PGB_StartNew = new PathGradientBrush(Path_StartNew)
            {
                CenterColor = Cr_StartNew,
                SurroundColors = new Color[] { Com.ColorManipulation.BlendByRGB(Cr_StartNew, EnterGameBackColor_DEC, 0.7) },
                FocusScales = new PointF(1F, 0F)
            };
            e.Graphics.FillPath(PGB_StartNew, Path_StartNew);
            Path_StartNew.Dispose();
            PGB_StartNew.Dispose();

            //

            if (Label_ContinueLastGame.Visible)
            {
                Rectangle Rect_Continue = new Rectangle(Label_ContinueLastGame.Location, Label_ContinueLastGame.Size);

                Color Cr_Continue = Com.ColorManipulation.BlendByRGB(EnterGameBackColor_INC, EnterGameBackColor_DEC, Math.Sqrt((double)(Label_ContinueLastGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min)));

                GraphicsPath Path_Continue = new GraphicsPath();
                Path_Continue.AddRectangle(Rect_Continue);
                PathGradientBrush PGB_Continue = new PathGradientBrush(Path_Continue)
                {
                    CenterColor = Cr_Continue,
                    SurroundColors = new Color[] { Com.ColorManipulation.BlendByRGB(Cr_Continue, EnterGameBackColor_DEC, 0.7) },
                    FocusScales = new PointF(1F, 0F)
                };
                e.Graphics.FillPath(PGB_Continue, Path_Continue);
                Path_Continue.Dispose();
                PGB_Continue.Dispose();
            }
        }

        private double EnterPrompt_Val = 0; // 闪烁相位。
        private double EnterPrompt_Step = 0.025; // 闪烁步长。

        private void Timer_EnterPrompt_Tick(object sender, EventArgs e)
        {
            //
            // Timer_EnterPrompt。
            //

            if (EnterPrompt_Val >= 0 && EnterPrompt_Val <= 1)
            {
                EnterPrompt_Val += EnterPrompt_Step;
            }

            if (EnterPrompt_Val < 0 || EnterPrompt_Val > 1)
            {
                EnterPrompt_Val = Math.Max(0, Math.Min(EnterPrompt_Val, 1));

                EnterPrompt_Step = -EnterPrompt_Step;
            }

            EnterGameBackColor_INC = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Border_INC, Me.RecommendColors.Border, EnterPrompt_Val).ToColor();

            //

            if (Label_ContinueLastGame.Visible)
            {
                Label_StartNewGame.Top = 0;

                if (Com.Geometry.CursorIsInControl(Label_StartNewGame))
                {
                    Label_StartNewGame.Height = Math.Max(EnterGameButtonHeight_Min, Math.Min(EnterGameButtonHeight_Max, Label_StartNewGame.Height + Math.Max(1, (EnterGameButtonHeight_Max - Label_StartNewGame.Height) / 4)));
                }
                else
                {
                    Label_StartNewGame.Height = Math.Max(EnterGameButtonHeight_Min, Math.Min(EnterGameButtonHeight_Max, Label_StartNewGame.Height - Math.Max(1, (Label_StartNewGame.Height - EnterGameButtonHeight_Min) / 4)));
                }

                Label_ContinueLastGame.Top = Label_StartNewGame.Bottom;
                Label_ContinueLastGame.Height = Panel_EnterGameSelection.Height - Label_ContinueLastGame.Top;
            }
            else
            {
                Label_StartNewGame.Height = EnterGameButtonHeight_Max;

                Label_StartNewGame.Top = (Panel_EnterGameSelection.Height - Label_StartNewGame.Height) / 2;
            }

            Label_StartNewGame.Width = (Int32)(Math.Sqrt((double)Label_StartNewGame.Height / EnterGameButtonHeight_Max) * Panel_EnterGameSelection.Width);
            Label_StartNewGame.Left = (Panel_EnterGameSelection.Width - Label_StartNewGame.Width) / 2;

            Label_ContinueLastGame.Width = (Int32)(Math.Sqrt((double)Label_ContinueLastGame.Height / EnterGameButtonHeight_Max) * Panel_EnterGameSelection.Width);
            Label_ContinueLastGame.Left = (Panel_EnterGameSelection.Width - Label_ContinueLastGame.Width) / 2;

            Label_StartNewGame.Font = new Font("微软雅黑", Math.Max(1F, (Label_StartNewGame.Height - 4) / 3F), FontStyle.Regular, GraphicsUnit.Point, 134);
            Label_ContinueLastGame.Font = new Font("微软雅黑", Math.Max(1F, (Label_ContinueLastGame.Height - 4) / 3F), FontStyle.Regular, GraphicsUnit.Point, 134);

            Label_StartNewGame.ForeColor = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Text_INC, Me.RecommendColors.Text, Math.Sqrt((double)(Label_StartNewGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min))).ToColor();
            Label_ContinueLastGame.ForeColor = Com.ColorManipulation.BlendByRGB(Me.RecommendColors.Text_INC, Me.RecommendColors.Text, Math.Sqrt((double)(Label_ContinueLastGame.Height - EnterGameButtonHeight_Min) / (EnterGameButtonHeight_Max - EnterGameButtonHeight_Min))).ToColor();

            //

            Panel_EnterGameSelection.Refresh();
        }

        #endregion

        #region "记录"区域

        private void PaintScore(PaintEventArgs e)
        {
            //
            // 绘制成绩。
            //

            Graphics Grap = e.Graphics;
            Grap.SmoothingMode = SmoothingMode.AntiAlias;

            //

            Int32 RectBottom = Panel_Score.Height - 50;

            Size RectSize_Max = new Size(Math.Max(2, Panel_Score.Width / 8), Math.Max(2, Panel_Score.Height - 120));
            Size RectSize_Min = new Size(Math.Max(2, Panel_Score.Width / 8), 2);

            Rectangle Rect_This = new Rectangle();
            Rectangle Rect_Best = new Rectangle();

            if (BestRecord.Score == 0)
            {
                Rect_Best.Size = new Size(RectSize_Max.Width, RectSize_Min.Height);
                Rect_This.Size = new Size(Rect_Best.Width, RectSize_Min.Height);
            }
            else
            {
                if (BestRecord.Score >= ThisRecord.Score)
                {
                    Rect_Best.Size = RectSize_Max;
                    Rect_This.Size = new Size(Rect_Best.Width, (Int32)Math.Max(RectSize_Min.Height, Math.Sqrt(ThisRecord.Score / BestRecord.Score) * Rect_Best.Height));
                }
                else
                {
                    Rect_This.Size = RectSize_Max;
                    Rect_Best.Size = new Size(Rect_This.Width, (Int32)Math.Max(RectSize_Min.Height, Math.Sqrt(BestRecord.Score / ThisRecord.Score) * Rect_This.Height));
                }
            }

            Rect_This.Location = new Point((Panel_Score.Width / 2 - Rect_This.Width) / 2, RectBottom - Rect_This.Height);
            Rect_Best.Location = new Point(Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Rect_Best.Width) / 2, RectBottom - Rect_Best.Height);

            Color RectCr = Me.RecommendColors.Border.ToColor();

            GraphicsPath Path_This = new GraphicsPath();
            Path_This.AddRectangle(Rect_This);
            PathGradientBrush PGB_This = new PathGradientBrush(Path_This)
            {
                CenterColor = RectCr,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr, 0.3) },
                FocusScales = new PointF(0F, 1F)
            };
            Grap.FillPath(PGB_This, Path_This);
            Path_This.Dispose();
            PGB_This.Dispose();

            GraphicsPath Path_Best = new GraphicsPath();
            Path_Best.AddRectangle(Rect_Best);
            PathGradientBrush PGB_Best = new PathGradientBrush(Path_Best)
            {
                CenterColor = RectCr,
                SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(RectCr, 0.3) },
                FocusScales = new PointF(0F, 1F)
            };
            Grap.FillPath(PGB_Best, Path_Best);
            Path_Best.Dispose();
            PGB_Best.Dispose();

            //

            Label_ThisRecordVal_PlatformCountAndAccuracy.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecordVal_PlatformCountAndAccuracy.Width, (Panel_Score.Width / 2 - Label_ThisRecordVal_PlatformCountAndAccuracy.Width) / 2));
            Label_ThisRecordVal_PlatformCountAndAccuracy.Top = Rect_This.Y - 5 - Label_ThisRecordVal_PlatformCountAndAccuracy.Height;
            Label_ThisRecordVal_Score.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_ThisRecordVal_Score.Width, (Panel_Score.Width / 2 - Label_ThisRecordVal_Score.Width) / 2));
            Label_ThisRecordVal_Score.Top = Label_ThisRecordVal_PlatformCountAndAccuracy.Top - Label_ThisRecordVal_Score.Height;

            Label_BestRecordVal_PlatformCountAndAccuracy.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecordVal_PlatformCountAndAccuracy.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecordVal_PlatformCountAndAccuracy.Width) / 2));
            Label_BestRecordVal_PlatformCountAndAccuracy.Top = Rect_Best.Y - 5 - Label_BestRecordVal_PlatformCountAndAccuracy.Height;
            Label_BestRecordVal_Score.Left = Math.Max(0, Math.Min(Panel_Score.Width - Label_BestRecordVal_Score.Width, Panel_Score.Width / 2 + (Panel_Score.Width / 2 - Label_BestRecordVal_Score.Width) / 2));
            Label_BestRecordVal_Score.Top = Label_BestRecordVal_PlatformCountAndAccuracy.Top - Label_BestRecordVal_Score.Height;
        }

        #endregion

        #region "选项"区域

        // 按压灵敏度。

        private const Int32 PressSensitivityMouseWheelStep = 5; // 按压灵敏度的鼠标滚轮调节步长。

        private bool PressSensitivityIsAdjusting = false; // 是否正在调整按压灵敏度。

        private Bitmap PressSensitivityTrbBmp; // 按压灵敏度调节器位图。

        private Size PressSensitivityTrbSliderSize => new Size(2, Panel_PressSensitivityAdjustment.Height); // 按压灵敏度调节器滑块大小。

        private void UpdatePressSensitivityTrbBmp()
        {
            //
            // 更新按压灵敏度调节器位图。
            //

            if (PressSensitivityTrbBmp != null)
            {
                PressSensitivityTrbBmp.Dispose();
            }

            PressSensitivityTrbBmp = new Bitmap(Math.Max(1, Panel_PressSensitivityAdjustment.Width), Math.Max(1, Panel_PressSensitivityAdjustment.Height));

            using (Graphics PressSensitivityTrbBmpGrap = Graphics.FromImage(PressSensitivityTrbBmp))
            {
                PressSensitivityTrbBmpGrap.Clear(Panel_PressSensitivityAdjustment.BackColor);

                //

                Color Color_Slider, Color_ScrollBar_Current, Color_ScrollBar_Unavailable;

                if (Com.Geometry.CursorIsInControl(Panel_PressSensitivityAdjustment) || PressSensitivityIsAdjusting)
                {
                    Color_Slider = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_INC, 0.3).ToColor();
                    Color_ScrollBar_Current = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_INC, 0.3).ToColor();
                    Color_ScrollBar_Unavailable = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_DEC, 0.3).ToColor();
                }
                else
                {
                    Color_Slider = Me.RecommendColors.Border_INC.ToColor();
                    Color_ScrollBar_Current = Me.RecommendColors.Border_INC.ToColor();
                    Color_ScrollBar_Unavailable = Me.RecommendColors.Border_DEC.ToColor();
                }

                Rectangle Rect_Slider = new Rectangle(new Point((Panel_PressSensitivityAdjustment.Width - PressSensitivityTrbSliderSize.Width) * (PressSensitivity - PressSensitivity_MIN) / (PressSensitivity_MAX - PressSensitivity_MIN), 0), PressSensitivityTrbSliderSize);
                Rectangle Rect_ScrollBar_Current = new Rectangle(new Point(0, 0), new Size(Rect_Slider.X, Panel_PressSensitivityAdjustment.Height));
                Rectangle Rect_ScrollBar_Unavailable = new Rectangle(new Point(Rect_Slider.Right, 0), new Size(Panel_PressSensitivityAdjustment.Width - Rect_Slider.Right, Panel_PressSensitivityAdjustment.Height));

                Rect_Slider.Width = Math.Max(1, Rect_Slider.Width);
                Rect_ScrollBar_Current.Width = Math.Max(1, Rect_ScrollBar_Current.Width);
                Rect_ScrollBar_Unavailable.Width = Math.Max(1, Rect_ScrollBar_Unavailable.Width);

                GraphicsPath Path_ScrollBar_Unavailable = new GraphicsPath();
                Path_ScrollBar_Unavailable.AddRectangle(Rect_ScrollBar_Unavailable);
                PathGradientBrush PGB_ScrollBar_Unavailable = new PathGradientBrush(Path_ScrollBar_Unavailable)
                {
                    CenterColor = Color_ScrollBar_Unavailable,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_ScrollBar_Unavailable, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PressSensitivityTrbBmpGrap.FillPath(PGB_ScrollBar_Unavailable, Path_ScrollBar_Unavailable);
                Path_ScrollBar_Unavailable.Dispose();
                PGB_ScrollBar_Unavailable.Dispose();

                GraphicsPath Path_ScrollBar_Current = new GraphicsPath();
                Path_ScrollBar_Current.AddRectangle(Rect_ScrollBar_Current);
                PathGradientBrush PGB_ScrollBar_Current = new PathGradientBrush(Path_ScrollBar_Current)
                {
                    CenterColor = Color_ScrollBar_Current,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_ScrollBar_Current, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PressSensitivityTrbBmpGrap.FillPath(PGB_ScrollBar_Current, Path_ScrollBar_Current);
                Path_ScrollBar_Current.Dispose();
                PGB_ScrollBar_Current.Dispose();

                GraphicsPath Path_Slider = new GraphicsPath();
                Path_Slider.AddRectangle(Rect_Slider);
                PathGradientBrush PGB_Slider = new PathGradientBrush(Path_Slider)
                {
                    CenterColor = Color_Slider,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_Slider, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PressSensitivityTrbBmpGrap.FillPath(PGB_Slider, Path_Slider);
                Path_Slider.Dispose();
                PGB_Slider.Dispose();
            }

            //

            Label_PressSensitivity_Val.Text = PressSensitivity + "%";
        }

        private void RepaintPressSensitivityTrbBmp()
        {
            //
            // 更新并重绘按压灵敏度调节器位图。
            //

            UpdatePressSensitivityTrbBmp();

            if (PressSensitivityTrbBmp != null)
            {
                Panel_PressSensitivityAdjustment.CreateGraphics().DrawImage(PressSensitivityTrbBmp, new Point(0, 0));
            }
        }

        private void PressSensitivityAdjustment()
        {
            //
            // 调整按压灵敏度。
            //

            Int32 CurPosXOfCtrl = Math.Max(-PressSensitivityTrbSliderSize.Width, Math.Min(Com.Geometry.GetCursorPositionOfControl(Panel_PressSensitivityAdjustment).X, Panel_PressSensitivityAdjustment.Width + PressSensitivityTrbSliderSize.Width));

            double DivisionWidth = (double)(Panel_PressSensitivityAdjustment.Width - PressSensitivityTrbSliderSize.Width) / (PressSensitivity_MAX - PressSensitivity_MIN);

            PressSensitivity = (Int32)Math.Max(PressSensitivity_MIN, Math.Min(PressSensitivity_MIN + (CurPosXOfCtrl - (PressSensitivityTrbSliderSize.Width - DivisionWidth) / 2) / DivisionWidth, PressSensitivity_MAX));

            RepaintPressSensitivityTrbBmp();
        }

        private void Panel_PressSensitivityAdjustment_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_PressSensitivityAdjustment 绘图。
            //

            UpdatePressSensitivityTrbBmp();

            if (PressSensitivityTrbBmp != null)
            {
                e.Graphics.DrawImage(PressSensitivityTrbBmp, new Point(0, 0));
            }
        }

        private void Panel_PressSensitivityAdjustment_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Panel_PressSensitivityAdjustment。
            //

            RepaintPressSensitivityTrbBmp();
        }

        private void Panel_PressSensitivityAdjustment_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Panel_PressSensitivityAdjustment。
            //

            RepaintPressSensitivityTrbBmp();
        }

        private void Panel_PressSensitivityAdjustment_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Panel_PressSensitivityAdjustment。
            //

            if (e.Button == MouseButtons.Left)
            {
                PressSensitivityIsAdjusting = true;

                PressSensitivityAdjustment();
            }
        }

        private void Panel_PressSensitivityAdjustment_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Panel_PressSensitivityAdjustment。
            //

            PressSensitivityIsAdjusting = false;
        }

        private void Panel_PressSensitivityAdjustment_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Panel_PressSensitivityAdjustment。
            //

            if (PressSensitivityIsAdjusting)
            {
                PressSensitivityAdjustment();
            }
        }

        private void Panel_PressSensitivityAdjustment_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 Panel_PressSensitivityAdjustment 滚动。
            //

            if (e.Delta > 0)
            {
                if (PressSensitivity % PressSensitivityMouseWheelStep == 0)
                {
                    PressSensitivity = Math.Min(PressSensitivity_MAX, PressSensitivity + PressSensitivityMouseWheelStep);
                }
                else
                {
                    PressSensitivity = Math.Min(PressSensitivity_MAX, PressSensitivity - PressSensitivity % PressSensitivityMouseWheelStep + PressSensitivityMouseWheelStep);
                }
            }
            else if (e.Delta < 0)
            {
                if (PressSensitivity % PressSensitivityMouseWheelStep == 0)
                {
                    PressSensitivity = Math.Max(PressSensitivity_MIN, PressSensitivity - PressSensitivityMouseWheelStep);
                }
                else
                {
                    PressSensitivity = Math.Max(PressSensitivity_MIN, PressSensitivity - PressSensitivity % PressSensitivityMouseWheelStep);
                }
            }

            RepaintPressSensitivityTrbBmp();
        }

        // 跳台不透明度。

        private const Int32 PlatformOpacityMouseWheelStep = 5; // 跳台不透明度的鼠标滚轮调节步长。

        private bool PlatformOpacityIsAdjusting = false; // 是否正在调整跳台不透明度。

        private Bitmap PlatformOpacityTrbBmp; // 跳台不透明度调节器位图。

        private Size PlatformOpacityTrbSliderSize => new Size(2, Panel_PlatformOpacityAdjustment.Height); // 跳台不透明度调节器滑块大小。

        private void UpdatePlatformOpacityTrbBmp()
        {
            //
            // 更新跳台不透明度调节器位图。
            //

            if (PlatformOpacityTrbBmp != null)
            {
                PlatformOpacityTrbBmp.Dispose();
            }

            PlatformOpacityTrbBmp = new Bitmap(Math.Max(1, Panel_PlatformOpacityAdjustment.Width), Math.Max(1, Panel_PlatformOpacityAdjustment.Height));

            using (Graphics PlatformOpacityTrbBmpGrap = Graphics.FromImage(PlatformOpacityTrbBmp))
            {
                PlatformOpacityTrbBmpGrap.Clear(Panel_PlatformOpacityAdjustment.BackColor);

                //

                Color Color_Slider, Color_ScrollBar_Current, Color_ScrollBar_Unavailable;

                if (Com.Geometry.CursorIsInControl(Panel_PlatformOpacityAdjustment) || PlatformOpacityIsAdjusting)
                {
                    Color_Slider = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_INC, 0.3).ToColor();
                    Color_ScrollBar_Current = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_INC, 0.3).ToColor();
                    Color_ScrollBar_Unavailable = Com.ColorManipulation.ShiftLightnessByHSL(Me.RecommendColors.Border_DEC, 0.3).ToColor();
                }
                else
                {
                    Color_Slider = Me.RecommendColors.Border_INC.ToColor();
                    Color_ScrollBar_Current = Me.RecommendColors.Border_INC.ToColor();
                    Color_ScrollBar_Unavailable = Me.RecommendColors.Border_DEC.ToColor();
                }

                Rectangle Rect_Slider = new Rectangle(new Point((Panel_PlatformOpacityAdjustment.Width - PlatformOpacityTrbSliderSize.Width) * (PlatformOpacity - PlatformOpacity_MIN) / (PlatformOpacity_MAX - PlatformOpacity_MIN), 0), PlatformOpacityTrbSliderSize);
                Rectangle Rect_ScrollBar_Current = new Rectangle(new Point(0, 0), new Size(Rect_Slider.X, Panel_PlatformOpacityAdjustment.Height));
                Rectangle Rect_ScrollBar_Unavailable = new Rectangle(new Point(Rect_Slider.Right, 0), new Size(Panel_PlatformOpacityAdjustment.Width - Rect_Slider.Right, Panel_PlatformOpacityAdjustment.Height));

                Rect_Slider.Width = Math.Max(1, Rect_Slider.Width);
                Rect_ScrollBar_Current.Width = Math.Max(1, Rect_ScrollBar_Current.Width);
                Rect_ScrollBar_Unavailable.Width = Math.Max(1, Rect_ScrollBar_Unavailable.Width);

                GraphicsPath Path_ScrollBar_Unavailable = new GraphicsPath();
                Path_ScrollBar_Unavailable.AddRectangle(Rect_ScrollBar_Unavailable);
                PathGradientBrush PGB_ScrollBar_Unavailable = new PathGradientBrush(Path_ScrollBar_Unavailable)
                {
                    CenterColor = Color_ScrollBar_Unavailable,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_ScrollBar_Unavailable, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PlatformOpacityTrbBmpGrap.FillPath(PGB_ScrollBar_Unavailable, Path_ScrollBar_Unavailable);
                Path_ScrollBar_Unavailable.Dispose();
                PGB_ScrollBar_Unavailable.Dispose();

                GraphicsPath Path_ScrollBar_Current = new GraphicsPath();
                Path_ScrollBar_Current.AddRectangle(Rect_ScrollBar_Current);
                PathGradientBrush PGB_ScrollBar_Current = new PathGradientBrush(Path_ScrollBar_Current)
                {
                    CenterColor = Color_ScrollBar_Current,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_ScrollBar_Current, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PlatformOpacityTrbBmpGrap.FillPath(PGB_ScrollBar_Current, Path_ScrollBar_Current);
                Path_ScrollBar_Current.Dispose();
                PGB_ScrollBar_Current.Dispose();

                GraphicsPath Path_Slider = new GraphicsPath();
                Path_Slider.AddRectangle(Rect_Slider);
                PathGradientBrush PGB_Slider = new PathGradientBrush(Path_Slider)
                {
                    CenterColor = Color_Slider,
                    SurroundColors = new Color[] { Com.ColorManipulation.ShiftLightnessByHSL(Color_Slider, 0.3) },
                    FocusScales = new PointF(1F, 0F)
                };
                PlatformOpacityTrbBmpGrap.FillPath(PGB_Slider, Path_Slider);
                Path_Slider.Dispose();
                PGB_Slider.Dispose();
            }

            //

            Label_PlatformOpacity_Val.Text = PlatformOpacity + "%";
        }

        private void RepaintPlatformOpacityTrbBmp()
        {
            //
            // 更新并重绘跳台不透明度调节器位图。
            //

            UpdatePlatformOpacityTrbBmp();

            if (PlatformOpacityTrbBmp != null)
            {
                Panel_PlatformOpacityAdjustment.CreateGraphics().DrawImage(PlatformOpacityTrbBmp, new Point(0, 0));
            }
        }

        private void PlatformOpacityAdjustment()
        {
            //
            // 调整跳台不透明度。
            //

            Int32 CurPosXOfCtrl = Math.Max(-PlatformOpacityTrbSliderSize.Width, Math.Min(Com.Geometry.GetCursorPositionOfControl(Panel_PlatformOpacityAdjustment).X, Panel_PlatformOpacityAdjustment.Width + PlatformOpacityTrbSliderSize.Width));

            double DivisionWidth = (double)(Panel_PlatformOpacityAdjustment.Width - PlatformOpacityTrbSliderSize.Width) / (PlatformOpacity_MAX - PlatformOpacity_MIN);

            PlatformOpacity = (Int32)Math.Max(PlatformOpacity_MIN, Math.Min(PlatformOpacity_MIN + (CurPosXOfCtrl - (PlatformOpacityTrbSliderSize.Width - DivisionWidth) / 2) / DivisionWidth, PlatformOpacity_MAX));

            RepaintPlatformOpacityTrbBmp();
        }

        private void Panel_PlatformOpacityAdjustment_Paint(object sender, PaintEventArgs e)
        {
            //
            // Panel_PlatformOpacityAdjustment 绘图。
            //

            UpdatePlatformOpacityTrbBmp();

            if (PlatformOpacityTrbBmp != null)
            {
                e.Graphics.DrawImage(PlatformOpacityTrbBmp, new Point(0, 0));
            }
        }

        private void Panel_PlatformOpacityAdjustment_MouseEnter(object sender, EventArgs e)
        {
            //
            // 鼠标进入 Panel_PlatformOpacityAdjustment。
            //

            RepaintPlatformOpacityTrbBmp();
        }

        private void Panel_PlatformOpacityAdjustment_MouseLeave(object sender, EventArgs e)
        {
            //
            // 鼠标离开 Panel_PlatformOpacityAdjustment。
            //

            RepaintPlatformOpacityTrbBmp();
        }

        private void Panel_PlatformOpacityAdjustment_MouseDown(object sender, MouseEventArgs e)
        {
            //
            // 鼠标按下 Panel_PlatformOpacityAdjustment。
            //

            if (e.Button == MouseButtons.Left)
            {
                PlatformOpacityIsAdjusting = true;

                PlatformOpacityAdjustment();
            }
        }

        private void Panel_PlatformOpacityAdjustment_MouseUp(object sender, MouseEventArgs e)
        {
            //
            // 鼠标释放 Panel_PlatformOpacityAdjustment。
            //

            PlatformOpacityIsAdjusting = false;
        }

        private void Panel_PlatformOpacityAdjustment_MouseMove(object sender, MouseEventArgs e)
        {
            //
            // 鼠标经过 Panel_PlatformOpacityAdjustment。
            //

            if (PlatformOpacityIsAdjusting)
            {
                PlatformOpacityAdjustment();
            }
        }

        private void Panel_PlatformOpacityAdjustment_MouseWheel(object sender, MouseEventArgs e)
        {
            //
            // 鼠标滚轮在 Panel_PlatformOpacityAdjustment 滚动。
            //

            if (e.Delta > 0)
            {
                if (PlatformOpacity % PlatformOpacityMouseWheelStep == 0)
                {
                    PlatformOpacity = Math.Min(PlatformOpacity_MAX, PlatformOpacity + PlatformOpacityMouseWheelStep);
                }
                else
                {
                    PlatformOpacity = Math.Min(PlatformOpacity_MAX, PlatformOpacity - PlatformOpacity % PlatformOpacityMouseWheelStep + PlatformOpacityMouseWheelStep);
                }
            }
            else if (e.Delta < 0)
            {
                if (PlatformOpacity % PlatformOpacityMouseWheelStep == 0)
                {
                    PlatformOpacity = Math.Max(PlatformOpacity_MIN, PlatformOpacity - PlatformOpacityMouseWheelStep);
                }
                else
                {
                    PlatformOpacity = Math.Max(PlatformOpacity_MIN, PlatformOpacity - PlatformOpacity % PlatformOpacityMouseWheelStep);
                }
            }

            RepaintPlatformOpacityTrbBmp();
        }

        // 主题颜色。

        private void RadioButton_UseRandomThemeColor_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_UseRandomThemeColor 选中状态改变。
            //

            if (RadioButton_UseRandomThemeColor.Checked)
            {
                UseRandomThemeColor = true;
            }

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;
        }

        private void RadioButton_UseCustomColor_CheckedChanged(object sender, EventArgs e)
        {
            //
            // RadioButton_UseCustomColor 选中状态改变。
            //

            if (RadioButton_UseCustomColor.Checked)
            {
                UseRandomThemeColor = false;
            }

            Label_ThemeColorName.Enabled = !UseRandomThemeColor;
        }

        private void Label_ThemeColorName_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_ThemeColorName。
            //

            ColorDialog_ThemeColor.Color = Me.ThemeColor.ToColor();

            Me.Enabled = false;

            DialogResult DR = ColorDialog_ThemeColor.ShowDialog();

            if (DR == DialogResult.OK)
            {
                Me.ThemeColor = new Com.ColorX(ColorDialog_ThemeColor.Color);
            }

            Me.Enabled = true;
        }

        // 抗锯齿。

        private void CheckBox_AntiAlias_CheckedChanged(object sender, EventArgs e)
        {
            //
            // CheckBox_AntiAlias 选中状态改变。
            //

            AntiAlias = CheckBox_AntiAlias.Checked;
        }

        #endregion

        #region "关于"区域

        private void Label_GitHub_Base_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_GitHub_Base。
            //

            Process.Start(URL_GitHub_Base);
        }

        private void Label_GitHub_Release_Click(object sender, EventArgs e)
        {
            //
            // 单击 Label_GitHub_Release。
            //

            Process.Start(URL_GitHub_Release);
        }

        #endregion

    }
}