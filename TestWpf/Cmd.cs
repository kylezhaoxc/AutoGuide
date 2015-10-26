using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWpf
{
    public class Cmd
    {
        public static readonly byte[] Reset =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._140,
                        0x08, 0x01, 0x00, 0x62, Angle._40,
                        0x08, 0x01, 0x00, 0x63, Angle._90,
                        0x08, 0x01, 0x00, 0x64, Angle._90};
        //public static readonly byte[] Reset =
        //    new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._140,
        //                0x08, 0x01, 0x00, 0x62, Angle._40,
        //                0x08, 0x01, 0x00, 0x63, Angle._90,
        //                0x08, 0x01, 0x00, 0x64, Angle._80};

        public static readonly byte[] debug =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._00,
                        0x08, 0x01, 0x00, 0x62, Angle._00,
                        0x08, 0x01, 0x00, 0x63, Angle._00,
                        0x08, 0x01, 0x00, 0x64, Angle._00};

        public static readonly byte[] SetLowPowerModel =
            new byte[] { 0x08, 0x01, 0x00, 0x01, 0x80 };

        public static readonly byte[] SetGeneralPowerModel =
            new byte[] { 0x08, 0x01, 0x00, 0x01, 0x00 };

        public static readonly byte[] ResetRobotStatus =
            new byte[] { 0x08, 0x01, 0x00, 0x31, 0x00 };

        public static readonly byte[] GetBattery =
            new byte[] { 0x08, 0x01, 0x00, 0x30, 0x8A };

        public static readonly byte[] GetCharggingStatus =
            new byte[] { 0x08, 0x01, 0x00, 0x30, 0x8B };

        public static readonly byte[] Charge =
            new byte[] { 0x08, 0x01, 0x00, 0x45, 0x80 };

        public static readonly byte[] OutCharge =
            new byte[] { 0x08, 0x01, 0x00, 0x45, 0x00 };

        public static readonly byte[] turnInnerRight =
            new byte[] { 0x08, 0x01, 0x00, 0x1F, 0xA8 };

        public static readonly byte[] turnInnerLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x1E, 0xA8 };

        public static readonly byte[] GetInnerTurningStatus =
            new byte[] { 0x08, 0x01, 0x00, 0x30, 0x1E };

        public static readonly byte[] FollowLeftWallCamera =
            new byte[] { 0x08, 0x01, 0x00, 0x64, Angle._60 };

        public static readonly byte[] FollowRightWallCamera =
            new byte[] { 0x08, 0x01, 0x00, 0x64, Angle._120 };

        public static readonly byte[] ReadyPassCornerLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._140,
                        0x08, 0x01, 0x00, 0x62, Angle._40,
                        0x08, 0x01, 0x00, 0x63, Angle._100,
                        0x08, 0x01, 0x00, 0x64, Angle._90};

        public static readonly byte[] ReadyPassCornerRight =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._140,
                          0x08, 0x01, 0x00, 0x62, Angle._40,
                          0x08, 0x01, 0x00, 0x63, Angle._70,
                          0x08, 0x01, 0x00, 0x64, Angle._90};

        public static readonly byte[] ReadyCharging =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._150,//96
                        0x08, 0x01, 0x00, 0x62, Angle._00,//00
                        0x08, 0x01, 0x00, 0x63, Angle._40,//1E
                        0x08, 0x01, 0x00, 0x64, Angle._40};//1E

        public static readonly byte[] ChargingMoveRight =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)(40),
                        0x08, 0x01, 0x00, 0x52, (byte)(90+128),
                        0x08, 0x01, 0x00, 0x53, (byte)(40)};

        public static readonly byte[] ChargingMoveLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)(40+128),
                                0x08, 0x01, 0x00, 0x52, (byte)(60),
                                0x08, 0x01, 0x00, 0x53, (byte)(40+128)};

        public static readonly byte[] Charging_turnLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x52, (byte)50 };

        public static readonly byte[] Charging_moveRight =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)(50 + 128) };

        public static readonly byte[] Charging_moveLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x53, (byte)(50) };

        public static readonly byte[] Charging_turnRight =
            new byte[] { 0x08, 0x01, 0x00, 0x52, (byte)(50 + 128) };

        public static readonly byte[] Charging_go =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)76,
                         0x08, 0x01, 0x00, 0x53, (byte)(76 + 128) };

        public static readonly byte[] Charging_stop =
            new byte[] { 0x08, 0x01, 0x00, 0x52, (byte)0 };

        public static readonly byte[] cameraLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x64, Angle._150 };

        public static readonly byte[] sensorLeft_lookB =
            new byte[] { 0x08, 0x01, 0x00, 0x62, Angle._80 };

        public static readonly byte[] sensorRight_lookB =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._100 };

        public static readonly byte[] sensorLeft_lookL =
            new byte[] { 0x08, 0x01, 0x00, 0x62, Angle._60 };

        public static readonly byte[] sensorRight_lookR =
            new byte[] { 0x08, 0x01, 0x00, 0x61, Angle._120 };


        static byte left_move = (byte)(70 + 128);
        static byte right_move = (byte)30;
        static byte font_move = (byte)50;

        public static readonly byte[] moveRight =
            new byte[] { 0x08, 0x01, 0x00, 0x51, right_move,
                0x08, 0x01, 0x00, 0x52, left_move,
                0x08, 0x01, 0x00, 0x53, font_move};

        public static readonly byte[] cameraRight =
            new byte[] { 0x08, 0x01, 0x00, 0x64, Angle._40 };

        public static readonly byte[] cameraFront =
            new byte[] { 0x08, 0x01, 0x00, 0x64, Angle._90 };

        public static readonly byte[] EnableSensor1 =
            new byte[] { 0x08, 0x01, 0x00, 0x71, 0x80 };
        public static readonly byte[] EnableSensor2 =
            new byte[] { 0x08, 0x01, 0x00, 0x72, 0x80 };
        public static readonly byte[] EnableSensor3 =
            new byte[] { 0x08, 0x01, 0x00, 0x73, 0x80 };

        public static readonly byte[] disEnableSensor1 =
            new byte[] { 0x08, 0x01, 0x00, 0x71, 0x00 };
        public static readonly byte[] disEnableSensor2 =
            new byte[] { 0x08, 0x01, 0x00, 0x72, 0x00 };
        public static readonly byte[] disEnableSensor3 =
            new byte[] { 0x08, 0x01, 0x00, 0x73, 0x00 };

        public static readonly byte[] StartAutoTrigger =
            new byte[] { 0x08, 0x01, 0x00, 0x70, 0x80, 0xF9 };

        public static readonly byte[] SpeedDown =
            new byte[] { 0x08, 0x01, 0x00, 0x41, 0x32 };
        public static readonly byte[] SpeedUp =
            new byte[] { 0x08, 0x01, 0x00, 0x41, 0x64 };

        static byte left = (byte)85;
        static byte right = (byte)(95 + 128);

        static byte speed75left = (byte)(65 + 128);
        static byte speed75right = (byte)65;

        public static readonly byte[] top_left_fast =
            new byte[] { 0x08, 0x01, 0x00, 0x53, speed75left };
        public static readonly byte[] top_right_fast =
            new byte[] { 0x08, 0x01, 0x00, 0x53, speed75right };


        public static readonly byte[] Go =
            new byte[] { 0x08, 0x01, 0x00, 0x51, right,
                0x08, 0x01, 0x00, 0x52, left};

        public static readonly byte[] GoFast =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)(100+128),
                0x08, 0x01, 0x00, 0x52, (byte)98};

        public static readonly byte[] GoR =
            new byte[] { 0x08, 0x01, 0x00, 0x51, (byte)(76 + 128),
                0x08, 0x01, 0x00, 0x52, left};

        static byte temp1 = (byte)(128 + 65);
        static byte temp2 = (byte)40;
        public static readonly byte[] GoRight =
            new byte[] { 0x08, 0x01, 0x00, 0x51, temp1,
                0x08, 0x01, 0x00, 0x52, temp2};

        public static readonly byte[] GoLeft =
            new byte[] { 0x08, 0x01, 0x00, 0x51, 0xB2,
                0x08, 0x01, 0x00, 0x52, 0x32};

        public static readonly byte[] checkStatus = new byte[] { 0x08, 0x01, 0x00, 0x30, 0x89 };
        public static readonly byte[] getRightSensor = new byte[] { 0x08, 0x01, 0x00, 0x30, 0x91 };
        public static readonly byte[] getLeftSensor = new byte[] { 0x08, 0x01, 0x00, 0x30, 0x92 };
        public static readonly byte[] getFontSensor = new byte[] { 0x08, 0x01, 0x00, 0x30, 0x93 };


        public static readonly byte[] stop = new byte[] { 0x08, 0x01, 0x00, 0x00, 0x00, 0x00 };
        public static readonly byte[] walk_right = new byte[] { 0x08, 0x01, 0x00, 0x1D, 0xA8, 0xBC };
        public static readonly byte[] walk_left = new byte[] { 0x08, 0x01, 0x00, 0x1B, 0xA8, 0xBC };

        public static readonly byte[] top_left = new byte[] { 0x08, 0x01, 0x00, 0x53, 0xB2 };
        public static readonly byte[] top_right = new byte[] { 0x08, 0x01, 0x00, 0x53, 0x32 };
        public static readonly byte[] top_stop = new byte[] { 0x08, 0x01, 0x00, 0x53, 0x00 };



        public static readonly byte[] turn_right = new byte[] {
            0x08, 0x01, 0x00, 0x41, 0x32,
            0x08, 0x01, 0x00, 0x51, 0x64,
            0x08, 0x01, 0x00, 0x52, 0x64,
            0x08, 0x01, 0x00, 0x53, 0x64};

        public static readonly byte[] turn_left = new byte[] {
            0x08, 0x01, 0x00, 0x41, 0x32,
            0x08, 0x01, 0x00, 0x51, 0xE4,
            0x08, 0x01, 0x00, 0x52, 0xE4,
            0x08, 0x01, 0x00, 0x53, 0xE4};




        public static readonly byte[] go_Horizontal_Left = new byte[] {
                0x08, 0x01, 0x00, 0x51, (byte)(40 + 128),
                0x08, 0x01, 0x00, 0x52, (byte)(40 + 128),
                0x08, 0x01, 0x00, 0x53, (byte)68};


        public static readonly byte[] go_Horizontal_Right = new byte[] {
                0x08, 0x01, 0x00, 0x51, (byte)(35),
                0x08, 0x01, 0x00, 0x52, (byte)(35),
                0x08, 0x01, 0x00, 0x53, (byte)(88 + 128)};


        public static readonly byte[] go_DepartureCharging = new byte[] {
                0x08, 0x01, 0x00, 0x51, (byte)(85),
                0x08, 0x01, 0x00, 0x53, (byte)(100 + 128)};
    }

    public class Angle
    {
        public static readonly byte _00 = (byte)00;
        public static readonly byte _10 = (byte)10;
        public static readonly byte _20 = (byte)20;
        public static readonly byte _30 = (byte)30;
        public static readonly byte _40 = (byte)40;
        public static readonly byte _50 = (byte)50;
        public static readonly byte _60 = (byte)60;
        public static readonly byte _70 = (byte)70;
        public static readonly byte _80 = (byte)80;
        public static readonly byte _90 = (byte)90;
        public static readonly byte _100 = (byte)100;
        public static readonly byte _110 = (byte)110;
        public static readonly byte _120 = (byte)120;
        public static readonly byte _130 = (byte)130;
        public static readonly byte _140 = (byte)140;
        public static readonly byte _150 = (byte)150;
        public static readonly byte _160 = (byte)160;
        public static readonly byte _170 = (byte)170;
        public static readonly byte _180 = (byte)180;
    }
}
