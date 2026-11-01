using FIGCommon.Utilities;

namespace FIG.Studies;

public partial class L2RuntimeRules : L2RuleScript
{
    public override List<L2BasicTrigger> GetBasicTriggerValue(L2TriggerContext context, bool getAll)
    {
        var MAC1 = context.MAC1;
        var MAC13 = context.MAC13;
        var MAC2 = context.MAC2;
        var MAC21 = context.MAC21;
        var MAC34 = context.MAC34;
        var MAC3 = context.MAC3;
        var MAC4 = context.MAC4;
        var MAC5 = context.MAC5;
        var MAC8 = context.MAC8;
        var MAC55 = context.MAC55;
        var MAC89 = context.MAC89;
        var MAL1 = context.MAL1;
        var MAL2 = context.MAL2;
        var MAL3 = context.MAL3;
        var SEMC1 = context.SEMC1;
        var SEMC13 = context.SEMC13;
        var SEMC34 = context.SEMC34;
        var SEMC2 = context.SEMC2;
        var SEMC21 = context.SEMC21;
        var SEMC3 = context.SEMC3;
        var SEMC4 = context.SEMC4;
        var SEMC5 = context.SEMC5;
        var SEMC8 = context.SEMC8;
        var SEMC55 = context.SEMC55;
        var SEMC89 = context.SEMC89;
        var LB2C13 = context.LB2C13;
        var LB2C21 = context.LB2C21;
        var LB2C34 = context.LB2C34;
        var LB2C3 = context.LB2C3;
        var LB2C4 = context.LB2C4;
        var LB2C5 = context.LB2C5;
        var LB2C8 = context.LB2C8;
        var LB2C89 = context.LB2C89;
        var UBC1 = context.UBC1;
        var LBC1 = context.LBC1;
        var LBC13 = context.LBC13;
        var LBC34 = context.LBC34;
        var LBC89 = context.LBC89;
        var LBC2 = context.LBC2;
        var LBC21 = context.LBC21;
        var LBC3 = context.LBC3;
        var LBC4 = context.LBC4;
        var LBC5 = context.LBC5;
        var LBC8 = context.LBC8;
        var UB2C1 = context.UB2C1;
        var UB2C5 = context.UB2C5;
        var UB2C4 = context.UB2C4;
        var UBC13 = context.UBC13;
        var UBC2 = context.UBC2;
        var UBC3 = context.UBC3;
        var UBC4 = context.UBC4;
        var UBC5 = context.UBC5;
        var UBC8 = context.UBC8;
        var UBC21 = context.UBC21;
        var UB2C13 = context.UB2C13;
        var UB2C21 = context.UB2C21;
        var UB2C8 = context.UB2C8;
        var C13_SEMTrend = context.C13_SEMTrend;
        var C3_SEMTrend = context.C3_SEMTrend;
        var C3_C13_S_Cont_LB1 = context.C3_C13_S_Cont_LB1;
        var C1_C5_S_Cont_LB1 = context.C1_C5_S_Cont_LB1;
        var C1_C5_S_Cont_LB2 = context.C1_C5_S_Cont_LB2;
        var C2_MAC_Pivot_Trend = context.C2_MAC_Pivot_Trend;
        var C2_SEMShortTrend = context.C2_SEMShortTrend;
        var C3_C13_Flag_Trend_Lower = context.C3_C13_Flag_Trend_Lower;
        var C3_C5_S_Cont_LB1 = context.C3_C5_S_Cont_LB1;
        var C3_C8_L_Cont_UB1 = context.C3_C8_L_Cont_UB1;
        var C3_C8_S_Cont_LB1 = context.C3_C8_S_Cont_LB1;
        var C2_C8_L_Cont_UB1 = context.C2_C8_L_Cont_UB1;
        var C3_MAC_Pivot_Trend = context.C3_MAC_Pivot_Trend;
        var C3_SEMShortTrend = context.C3_SEMShortTrend;
        var C21_SEMTrend = context.C21_SEMTrend;
        var C5_C21_Flag_Trend_Lower = context.C5_C21_Flag_Trend_Lower;
        var C5_C2_MATrend = context.C5_C2_MATrend;
        var C5_C3_MATrend = context.C5_C3_MATrend;
        var C5_MAC_Pivot_Trend = context.C5_MAC_Pivot_Trend;
        var C5_Pivot_Status = context.C5_Pivot_Status;
        var C5_Pivot_Status_Odd = context.C5_Pivot_Status_Odd;
        var C21_Pivot_Status = context.C21_Pivot_Status;
        var C5_SEMTrend = context.C5_SEMTrend;
        var C4_SEMTrend = context.C4_SEMTrend;
        var L1_C2_S_Cont_LB1 = context.L1_C2_S_Cont_LB1;
        var L1_C2_S_Cont_LB2 = context.L1_C2_S_Cont_LB2;
        var L1_C3_S_Cont_LB1 = context.L1_C3_S_Cont_LB1;
        var L1_C3_S_Cont_LB2 = context.L1_C3_S_Cont_LB2;
        var L2_MAC_Pivot_Trend = context.L2_MAC_Pivot_Trend;
        var C2_MAC_SupRes = context.C2_MAC_SupRes;
        var C3_MAC_SupRes = context.C3_MAC_SupRes;
        var C5_MAC_SupRes = context.C5_MAC_SupRes;
        var L2_MAC_SupRes = context.L2_MAC_SupRes;
        var C8_SEMTrend = context.C8_SEMTrend;
        var C21_UBLB1_Dir = context.C21_UBLB1_Dir;
        var C21_UBLB2_Dir = context.C21_UBLB2_Dir;
        var C13_UBLB1_Dir = context.C13_UBLB1_Dir;
        var C2_C5_LLB2_Cont_MAC = context.C2_C5_LLB2_Cont_MAC;
        var pC2_C5_LLB2_Cont_MAC = context.pC2_C5_LLB2_Cont_MAC;
        var LB2C2 = context.LB2C2;
        var C3_C5_LLB2_Cont_MAC = context.C3_C5_LLB2_Cont_MAC;
        var LB2C1 = context.LB2C1;
        var C2_C8_S_Cont_MAC = context.C2_C8_S_Cont_MAC;
        var C13_UBLB2_Dir = context.C13_UBLB2_Dir;
        var cParam = context.cParam;
        List<L2BasicTrigger>? matches = getAll ? new List<L2BasicTrigger>() : null;

        if (SEMC1 < MAC1 && SEMC1 < LB2C5
            && (MAL2 < SEMC3 || MAL2 < SEMC4) && MAL2 < MAC1
            && SEMC3 < MAC3 && SEMC5 < MAC5
            && MAC1 > SEMC2 && MAC1 > SEMC3 && MAC1 > SEMC4 && MAC1 > SEMC5
            && MAL2 > SEMC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.99M)];
            matches!.Add(CreateBasicTrigger(context, 4.99M));
        }
        if (SEMC21 < LBC13 && SEMC13 < LBC8 && SEMC8 < LBC5 && SEMC5 < LBC5 && SEMC5 > SEMC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.36M)];
            matches!.Add(CreateBasicTrigger(context, 6.36M));
        }

        if (SEMC5 > UBC21 && MAC5 > UBC21 && SEMC21 > UBC21 && SEMC5 < MAC5 && SEMC8 > MAC8 && SEMC13 > MAC13 && !(MAC8 > UBC21 && SEMC21 > UBC21 && SEMC8 < MAC8 && SEMC13 > MAC13))
        {
            if (SEMC8 > UBC8 && SEMC1 < UBC8 && SEMC2 < UBC8 && SEMC3 < UBC8 && SEMC4 < UBC8 && SEMC5 < UBC8 && SEMC13 < UBC8 && SEMC21 < UBC8)
            {
                if (!getAll)
                    return [CreateBasicTrigger(context, 4.01M)];
                matches!.Add(CreateBasicTrigger(context, 4.01M));
            }
            else
            {
                if (!getAll)
                    return [CreateBasicTrigger(context, 6.66M)];
                matches!.Add(CreateBasicTrigger(context, 6.66M));
            }
        }

        if (SEMC5 > MAC5 && SEMC5 < UBC5 && SEMC1 > MAC1 && UBC1 < MAC5 && ((SEMC5 > SEMC8) || (MAL2 > SEMC8)))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.16M)];
            matches!.Add(CreateBasicTrigger(context, 7.16M));
        }

        if (MAC3 > UBC8 && MAL2 < LBC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.29M)];
            matches!.Add(CreateBasicTrigger(context, 7.29M));
        }

        if (SEMC2 > UB2C5 && SEMC1 > UB2C5 && SEMC1 < SEMC2 && SEMC4 > UBC4 && SEMC5 > UBC5 && SEMC8 > UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.26M)];
            matches!.Add(CreateBasicTrigger(context, 7.26M));
        }

        if (SEMC8 < LBC5 && SEMC13 < LBC8 && SEMC21 < LBC13 && SEMC34 < LBC21 && SEMC8 < SEMC13 && SEMC13 < SEMC21 && SEMC21 < SEMC34 && SEMC5 < LB2C13 && SEMC5 < LB2C21 && MAL1 < LB2C13 && MAL1 < LB2C21 && MAL2 < LB2C13 && MAL2 < LB2C21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.24M)];
            matches!.Add(CreateBasicTrigger(context, 7.24M));
        }

        if (SEMC21 < LBC21 && SEMC13 > LBC13 && SEMC13 < MAC13 && SEMC13 < SEMC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.48M)];
            matches!.Add(CreateBasicTrigger(context, 7.48M));
        }

        if (SEMC21 < MAC21 && SEMC21 > LBC21 && cParam.C21_SEMLB1LB2_Stage == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.45M)];
            matches!.Add(CreateBasicTrigger(context, 7.45M));
        }

        if (SEMC21 < MAC21 && SEMC21 > LBC21 && SEMC13 < LBC13 && SEMC5 < LBC5 && SEMC13 < LBC8 && SEMC8 < LBC5 && MAL2 < SEMC8 && MAL2 < SEMC13 && MAL2 > LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.42M)];
            matches!.Add(CreateBasicTrigger(context, 7.42M));
        }

        if (SEMC21 < MAC21 && SEMC21 > LBC21 && SEMC13 < LBC13 && SEMC5 < MAC5 && SEMC5 < SEMC21 && SEMC21 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.43M)];
            matches!.Add(CreateBasicTrigger(context, 7.43M));
        }

        if (SEMC21 < MAC21 && SEMC21 > LBC21 && SEMC13 < LBC13 && MAC21 > MAC89 && MAL2 > LB2C21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.47M)];
            matches!.Add(CreateBasicTrigger(context, 7.47M));
        }

        if ((C2_C8_L_Cont_UB1 == 1 && C3_C8_L_Cont_UB1 == 1) && SEMC3 > SEMC5 && (MAL2 > MAC1 || SEMC1 > MAC1))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.92M)];
            matches!.Add(CreateBasicTrigger(context, 4.92M));
        }

        if (SEMC21 > MAC21 && SEMC21 < UBC21 && SEMC13 < LBC13 && MAC21 < MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.52M)];
            matches!.Add(CreateBasicTrigger(context, 7.52M));
        }

        if (SEMC21 > MAC21 && SEMC21 < UBC21 && SEMC13 < LBC13 && MAC21 > MAC89 && MAC13 > MAC89 && SEMC8 < LBC8 && MAC1 > LB2C21 && SEMC8 < LB2C13 && SEMC5 < LB2C13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.51M)];
            matches!.Add(CreateBasicTrigger(context, 7.51M));
        }

        if (SEMC21 > MAC21 && SEMC21 < UBC21 && SEMC13 < LBC13 && MAC21 > MAC89 && MAC13 > MAC89 && SEMC8 < LBC8 && MAC1 > LB2C21 && SEMC5 > SEMC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.49M)];
            matches!.Add(CreateBasicTrigger(context, 7.49M));
        }

        if (MAC2 < LBC13 && SEMC8 < MAC8 && SEMC8 > LBC8 && SEMC13 < MAC13 && SEMC5 < LBC5 && SEMC21 < UBC21 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.55M)];
            matches!.Add(CreateBasicTrigger(context, 7.55M));
        }

        if (MAC2 < LBC13 && SEMC8 < MAC8 && SEMC8 > LBC8 && SEMC13 < MAC13 && SEMC5 < LBC5 && SEMC21 < MAC21 && C21_UBLB2_Dir == 1 && C21_UBLB1_Dir == 1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.54M)];
            matches!.Add(CreateBasicTrigger(context, 7.54M));
        }

        if (SEMC3 > UB2C5 && SEMC2 > UB2C5 && SEMC1 > UB2C5 && SEMC5 > UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.69M)];
            matches!.Add(CreateBasicTrigger(context, 7.69M));
        }

        if (SEMC13 < MAC13 && SEMC5 < UBC5 && SEMC8 < UBC8 && (MAL2 > UB2C13 || MAL1 > UB2C13 || MAL3 > UB2C13))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.59M)];
            matches!.Add(CreateBasicTrigger(context, 7.59M));
        }

        if (MAL2 < MAC21 && SEMC13 < MAC13 && SEMC21 > UBC21 && C13_UBLB1_Dir == 1 && C13_UBLB2_Dir == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.62M)];
            matches!.Add(CreateBasicTrigger(context, 7.62M));
        }

        if (SEMC13 < LBC13 && SEMC21 > UBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.58M)];
            matches!.Add(CreateBasicTrigger(context, 7.58M));
        }

        if (MAL2 < MAC21 && SEMC13 < MAC13 && C13_UBLB1_Dir == 1 && C13_UBLB2_Dir == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.61M)];
            matches!.Add(CreateBasicTrigger(context, 7.61M));
        }

        if (SEMC21 > UBC21 && MAL2 < LB2C8 && MAL2 < LB2C13 && MAC1 > LB2C13 && MAC1 > LB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.74M)];
            matches!.Add(CreateBasicTrigger(context, 7.74M));
        }

        if (SEMC5 < MAC5 && LB2C5 > MAC8 && MAL2 < SEMC5 && MAC5 > MAC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.02M)];
            matches!.Add(CreateBasicTrigger(context, 4.02M));
        }

        if (SEMC8 > UBC8 && SEMC5 < LBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.14M)];
            matches!.Add(CreateBasicTrigger(context, 4.14M));
        }

        if (SEMC8 > UBC8 && SEMC1 < UBC8 && SEMC2 < UBC8 && SEMC3 < UBC8 && SEMC4 < UBC8 && SEMC5 < UBC8 && SEMC13 < UBC8 && SEMC21 < UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.01M)];
            matches!.Add(CreateBasicTrigger(context, 4.01M));
        }

        if (SEMC5 > MAC5 && SEMC5 < UBC5 && SEMC1 > MAC1 && UBC1 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.15M)];
            matches!.Add(CreateBasicTrigger(context, 7.15M));
        }

        if (SEMC5 > UBC5 && SEMC3 < LBC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.05M)];
            matches!.Add(CreateBasicTrigger(context, 4.05M));
        }

        if (SEMC5 > UBC5 && SEMC1 < UBC5 && SEMC2 < UBC5 && SEMC3 < UBC5 && SEMC4 < UBC5 && SEMC13 < UBC5 && SEMC21 < UBC5 && SEMC8 < UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.36M)];
            matches!.Add(CreateBasicTrigger(context, 7.36M));
        }

        if (SEMC5 > UBC5 && SEMC1 < UBC5 && SEMC2 < UBC5 && SEMC3 < UBC5 && SEMC4 < UBC5 && SEMC13 < UBC5 && SEMC21 < UBC5 && SEMC8 > UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.35M)];
            matches!.Add(CreateBasicTrigger(context, 7.35M));
        }

        if (SEMC13 > UBC13 && SEMC8 < LBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.08M)];
            matches!.Add(CreateBasicTrigger(context, 4.08M));
        }

        if (SEMC13 > UBC13 && SEMC8 > UBC8 && SEMC21 < UBC21 && SEMC8 < SEMC13 && SEMC5 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.07M)];
            matches!.Add(CreateBasicTrigger(context, 4.07M));
        }

        if (SEMC21 < UBC21 && SEMC21 < UB2C5 && SEMC21 > UBC5 && MAL2 < SEMC21 && SEMC13 < SEMC21 && SEMC8 < SEMC21 && SEMC5 < SEMC21 && SEMC5 < UBC5 && SEMC5 > MAC5 && SEMC13 < MAC13 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.34M)];
            matches!.Add(CreateBasicTrigger(context, 4.34M));
        }

        if (SEMC8 < LBC8 && SEMC5 < LBC5 && SEMC3 < LBC3 && SEMC2 < LBC2 && SEMC1 < LBC1 && SEMC21 > MAC21 && SEMC13 > LBC13 && SEMC8 < LBC5 && SEMC8 < MAC2 && SEMC5 < LBC3 && MAL2 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.36M)];
            matches!.Add(CreateBasicTrigger(context, 4.36M));
        }

        if (MAC5 > MAC89 && MAC13 > MAC21 && SEMC21 > MAC21 && MAC3 < LBC13 && SEMC13 < LBC13 && SEMC13 < MAC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.48M)];
            matches!.Add(CreateBasicTrigger(context, 4.48M));
        }

        if (MAC2 > MAC3 && MAC3 > MAC5 && MAC5 < MAC13 && SEMC5 > MAC5 && SEMC8 < MAC8 && SEMC13 < LBC13 && SEMC5 > SEMC13 && SEMC8 > SEMC13 && SEMC2 < LBC2 && SEMC1 < MAC1 && MAL2 > SEMC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.43M)];
            matches!.Add(CreateBasicTrigger(context, 4.43M));
        }

        if (SEMC1 < LB2C2 && SEMC2 < LB2C3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.47M)];
            matches!.Add(CreateBasicTrigger(context, 4.47M));
        }

        if (SEMC5 < LBC5 && UBC1 < SEMC5 && UBC1 < SEMC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.52M)];
            matches!.Add(CreateBasicTrigger(context, 4.52M));
        }

        if (SEMC5 < LB2C13 && SEMC5 < LB2C21 && MAC2 > MAC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.75M)];
            matches!.Add(CreateBasicTrigger(context, 4.75M));
        }

        if (SEMC5 < LB2C13 && SEMC5 < LB2C21 && UBC1 < LBC4)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.71M)];
            matches!.Add(CreateBasicTrigger(context, 4.71M));
        }

        if (MAC2 < LB2C13 && MAC2 < LBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.67M)];
            matches!.Add(CreateBasicTrigger(context, 4.67M));
        }

        if (SEMC5 < LB2C13 && SEMC5 < LB2C21 && (MAC1 > MAC3 || MAC1 > MAC2))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.74M)];
            matches!.Add(CreateBasicTrigger(context, 4.74M));
        }

        if (MAC2 < LB2C21 && MAC3 < LB2C21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.73M)];
            matches!.Add(CreateBasicTrigger(context, 4.73M));
        }

        if (MAL1 < LB2C5 && MAL2 < LB2C4 && MAL3 < LB2C2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.87M)];
            matches!.Add(CreateBasicTrigger(context, 4.87M));
        }

        if (SEMC5 > MAC5 && SEMC5 < UBC5 && SEMC8 > MAC8 && SEMC8 < UBC8 && SEMC5 < SEMC8 && C8_SEMTrend != -0.1M && C5_SEMTrend != -0.1M)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.13M)];
            matches!.Add(CreateBasicTrigger(context, 7.13M));
        }

        if (MAL1 > LB2C5 && MAL2 > LB2C4 && MAL3 < LB2C2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.85M)];
            matches!.Add(CreateBasicTrigger(context, 4.85M));
        }

        if (SEMC5 < MAC5 && SEMC8 < UBC8 && SEMC8 > MAC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && SEMC21 < MAC21 && SEMC21 > LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.19M)];
            matches!.Add(CreateBasicTrigger(context, 7.19M));
        }

        if (MAL2 < LB2C13 && MAC8 > MAC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.93M)];
            matches!.Add(CreateBasicTrigger(context, 7.93M));
        }

        if (SEMC2 < LB2C4 && SEMC1 > MAC1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.88M)];
            matches!.Add(CreateBasicTrigger(context, 4.88M));
        }

        if (MAL2 < LB2C3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.86M)];
            matches!.Add(CreateBasicTrigger(context, 4.86M));
        }

        if (SEMC1 < LB2C2 && SEMC2 < LB2C4 && SEMC3 > LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.82M)];
            matches!.Add(CreateBasicTrigger(context, 4.82M));
        }

        if (SEMC4 < LBC2 && SEMC2 < LB2C5 && SEMC3 > LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.83M)];
            matches!.Add(CreateBasicTrigger(context, 4.83M));
        }

        if (MAC2 < MAC5 && MAC1 > SEMC4 && UBC1 < SEMC5 && SEMC2 < LB2C4 && MAL2 > LB2C8 && MAL2 > LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.84M)];
            matches!.Add(CreateBasicTrigger(context, 4.84M));
        }

        if (MAC1 > SEMC3 && MAC1 > SEMC2 && MAC2 > LBC13 && SEMC5 > LB2C8 && (pC2_C5_LLB2_Cont_MAC == 1 || C2_C5_LLB2_Cont_MAC == 1))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.94M)];
            matches!.Add(CreateBasicTrigger(context, 4.94M));
        }

        if (MAC2 > LBC13 && (C3_C5_LLB2_Cont_MAC == 1))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.91M)];
            matches!.Add(CreateBasicTrigger(context, 4.91M));
        }

        if (SEMC3 <= Math.Min(LB2C13, LB2C21) && MAL2 > LB2C5 && MAC1 > SEMC4 && MAC1 > SEMC5 && SEMC2 < LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.93M)];
            matches!.Add(CreateBasicTrigger(context, 4.93M));
        }

        if (SEMC3 < LB2C5)
        {
            var basicTrigger = ResolveBasicTrigger498();
            if (!getAll)
                return [CreateBasicTrigger(context, basicTrigger)];
            matches!.Add(CreateBasicTrigger(context, basicTrigger));
        }

        if (LB2C1 > UBC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.84M)];
            matches!.Add(CreateBasicTrigger(context, 7.84M));
        }

        if (MAL2 > UB2C13 && MAL2 > UB2C21 && MAC1 > UB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.37M)];
            matches!.Add(CreateBasicTrigger(context, 6.37M));
        }

        if (SEMC13 > UBC13 && SEMC8 > UBC8 && SEMC21 < UBC21 && SEMC8 < SEMC13 && SEMC5 < UBC5 && SEMC5 > MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.06M)];
            matches!.Add(CreateBasicTrigger(context, 4.06M));
        }

        if (L2_MAC_Pivot_Trend == -1 && C2_MAC_Pivot_Trend == -1 && C3_MAC_Pivot_Trend == -1 && C5_MAC_Pivot_Trend == 1 && C5_C3_MATrend == -1 && C5_C2_MATrend == -1 && L2_MAC_SupRes > C5_MAC_SupRes && C2_MAC_SupRes > C5_MAC_SupRes && (L2_MAC_SupRes > C2_MAC_SupRes || L2_MAC_SupRes > C3_MAC_SupRes))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.9M)];
            matches!.Add(CreateBasicTrigger(context, 6.9M));
        }

        if (SEMC2 > UB2C5 && SEMC3 > UB2C5 && SEMC2 > UB2C1 && SEMC3 > UB2C1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.73M)];
            matches!.Add(CreateBasicTrigger(context, 6.73M));
        }

        if (SEMC1 > SEMC5 && SEMC1 < MAC1 && SEMC5 > UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.77M)];
            matches!.Add(CreateBasicTrigger(context, 6.77M));
        }

        if (SEMC5 < SEMC8 && SEMC5 < SEMC13 && SEMC5 < SEMC21 && SEMC5 > UBC5 && SEMC8 > UBC8 && SEMC13 > UBC13 && SEMC21 > UBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.76M)];
            matches!.Add(CreateBasicTrigger(context, 6.76M));
        }

        if (MAC1 > UB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.75M)];
            matches!.Add(CreateBasicTrigger(context, 6.75M));
        }

        if (C5_Pivot_Status_Odd > 0)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.99M)];
            matches!.Add(CreateBasicTrigger(context, 7.99M));
        }

        if ((C1_C5_S_Cont_LB2 == -1 && C1_C5_S_Cont_LB1 == -1))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.99M)];
            matches!.Add(CreateBasicTrigger(context, 1.99M));
        }

        if (C1_C5_S_Cont_LB2 == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.98M)];
            matches!.Add(CreateBasicTrigger(context, 1.98M));
        }

        if (L1_C2_S_Cont_LB2 == -1 && L1_C2_S_Cont_LB1 == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.97M)];
            matches!.Add(CreateBasicTrigger(context, 1.97M));
        }

        if (L1_C2_S_Cont_LB2 == -1 && L1_C3_S_Cont_LB2 == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.97M)];
            matches!.Add(CreateBasicTrigger(context, 1.97M));
        }

        if (L1_C3_S_Cont_LB1 == -1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.96M)];
            matches!.Add(CreateBasicTrigger(context, 1.96M));
        }

        if ((C2_C8_S_Cont_MAC == -1))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.95M)];
            matches!.Add(CreateBasicTrigger(context, 1.95M));
        }

        if (C3_C8_S_Cont_LB1 == -1 && C3_C5_S_Cont_LB1 == -1 && MAC1 < MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 1.94M)];
            matches!.Add(CreateBasicTrigger(context, 1.94M));
        }

        if (MAL2 > UB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.38M)];
            matches!.Add(CreateBasicTrigger(context, 6.38M));
        }
        if (MAC13 > MAC21 && MAC21 > MAC34
            && SEMC5 > MAC5 && SEMC8 > MAC8
            && SEMC13 < MAC13 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.26M)];
            matches!.Add(CreateBasicTrigger(context, 6.26M));
        }

        if (SEMC5 > UB2C8 && SEMC5 > UB2C13 && SEMC5 > UB2C21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.05M)];
            matches!.Add(CreateBasicTrigger(context, 7.05M));
        }

        if (SEMC5 > UB2C8 && SEMC1 > UB2C8 && SEMC1 > MAC1 && (SEMC5 < UB2C13 || SEMC5 < UB2C21))
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.01M)];
            matches!.Add(CreateBasicTrigger(context, 7.01M));
        }

        if (SEMC1 > UBC1 && SEMC5 >= MAC5 && SEMC5 < UBC5 && UBC1 < SEMC5 && UB2C1 > SEMC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.72M)];
            matches!.Add(CreateBasicTrigger(context, 6.72M));
        }

        if (C5_SEMTrend != -0.1M && SEMC5 > SEMC8 && SEMC5 > SEMC13 && MAL2 > SEMC5 && MAL2 > UBC5 && SEMC5 > UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.06M)];
            matches!.Add(CreateBasicTrigger(context, 7.06M));
        }

        if (SEMC8 > UBC8 && MAC2 < UBC8 && MAC1 < UBC8 && SEMC3 > UBC8 && SEMC4 > UBC8 && SEMC5 > UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.29M)];
            matches!.Add(CreateBasicTrigger(context, 4.29M));
        }

        if (SEMC21 < LBC21 && SEMC13 > UBC13 && SEMC8 > UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.31M)];
            matches!.Add(CreateBasicTrigger(context, 4.31M));
        }

        if (LBC2 > UBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.86M)];
            matches!.Add(CreateBasicTrigger(context, 7.86M));
        }

        if (MAC1 > SEMC5 && MAL2 > SEMC5 && SEMC1 < MAC1 && MAL2 < MAC1 && MAC3 > MAC5 && MAC2 > MAC3 && MAC1 > MAC2 && SEMC5 > MAC5 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 > MAC5 && MAL2 > MAC3 && MAL2 > MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.89M)];
            matches!.Add(CreateBasicTrigger(context, 7.89M));
        }

        if (MAC1 > SEMC5 && MAL2 <= SEMC5 && SEMC1 < MAC1 && MAL2 < MAC1 && MAC3 > MAC5 && MAC2 > MAC3 && MAC1 > MAC2 && SEMC5 > MAC5 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 > MAC5 && MAL2 > MAC3 && MAL2 > MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.88M)];
            matches!.Add(CreateBasicTrigger(context, 7.88M));
        }

        if (SEMC5 > MAC5 && SEMC5 < UBC5 && C5_SEMTrend != -0.1M && SEMC1 > SEMC5 && SEMC1 > MAC1 && SEMC4 < MAC1 && MAL2 > MAC1 && MAL2 > MAC2 && MAL2 > MAC3 && MAL2 > MAC5 && MAL2 > SEMC5 && MAL2 > SEMC3 && MAC1 > MAC2 && MAC1 > MAC3 && SEMC1 > SEMC3 && SEMC1 > SEMC4 && SEMC2 > SEMC3 && SEMC2 > SEMC5 && MAC2 > MAC3 && SEMC2 > MAC2 && MAC3 < MAC5 && SEMC3 > SEMC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.91M)];
            matches!.Add(CreateBasicTrigger(context, 7.91M));
        }

        if (SEMC4 > SEMC5 && MAC1 > UBC5 && MAC1 > UBC4 && SEMC4 < UBC4 && SEMC5 < UBC5 && C4_SEMTrend == 1 && C5_SEMTrend == 1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.74M)];
            matches!.Add(CreateBasicTrigger(context, 6.74M));
        }

        if (SEMC5 < MAC5 && MAL2 < MAC1 && SEMC1 < MAC1 && MAL2 < SEMC2 && C2_SEMShortTrend != 1 && MAL3 < SEMC1 && MAL3 < LBC1 && SEMC2 < MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.94M)];
            matches!.Add(CreateBasicTrigger(context, 7.94M));
        }

        if (MAL2 > MAC5 && MAL2 > MAC1 && MAC1 > MAC5 && SEMC1 > MAC1 && SEMC1 > MAL2 && SEMC1 < SEMC8 && MAC2 < MAC5 && SEMC2 > MAC2 && SEMC2 < SEMC8 && MAC3 < MAC5 && SEMC3 > MAC3 && SEMC3 < SEMC8 && SEMC5 < MAC5 && SEMC5 < SEMC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.97M)];
            matches!.Add(CreateBasicTrigger(context, 7.97M));
        }

        if (SEMC21 < LBC21 && SEMC13 < LBC13 && SEMC8 > MAC8 && SEMC5 < MAC5 && SEMC13 > SEMC21 && SEMC8 > SEMC21 && SEMC5 > SEMC21 && SEMC5 > SEMC13 && SEMC8 > SEMC13 && MAC5 > MAC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.35M)];
            matches!.Add(CreateBasicTrigger(context, 6.35M));
        }

        if (MAC8 > UBC21 && SEMC21 > UBC21 && SEMC8 < MAC8 && SEMC13 > MAC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.64M)];
            matches!.Add(CreateBasicTrigger(context, 6.64M));
        }

        if (SEMC2 > MAC2 && SEMC5 < MAC5 && MAL2 < MAC1 && SEMC1 < MAC1 && SEMC3 < MAC3 && SEMC4 < MAC4 && MAL2 < SEMC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.95M)];
            matches!.Add(CreateBasicTrigger(context, 7.95M));
        }

        if (SEMC2 < MAC2 && SEMC5 < MAC5 && MAL2 < MAC1 && SEMC1 < MAC1 && SEMC3 < MAC3 && SEMC4 < MAC4 && MAL2 < SEMC2 && MAL3 < LB2C3 && SEMC3 > SEMC5 && SEMC4 > SEMC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.92M)];
            matches!.Add(CreateBasicTrigger(context, 7.92M));
        }

        if (SEMC2 < MAC2 && SEMC5 < MAC5 && MAL2 < MAC1 && SEMC1 < MAC1 && SEMC3 < MAC3 && SEMC4 < MAC4 && MAL2 < SEMC2 && C3_SEMShortTrend != 1 && C2_SEMShortTrend != 1 && C2_MAC_Pivot_Trend == -1 && SEMC2 > LB2C4 && SEMC2 > LB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.96M)];
            matches!.Add(CreateBasicTrigger(context, 7.96M));
        }

        if (MAL2 < LB2C5 && MAC3 > MAC13 && MAL2 >= MAC13 && (MAL3 > LBC1 || MAL3 > LB2C3) && SEMC3 < MAC3 && MAL2 > LB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.76M)];
            matches!.Add(CreateBasicTrigger(context, 4.76M));
        }

        if (MAL1 < LB2C8 && MAL1 < LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.78M)];
            matches!.Add(CreateBasicTrigger(context, 4.78M));
        }

        if (MAL2 < LB2C8 && MAL2 < LB2C5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.77M)];
            matches!.Add(CreateBasicTrigger(context, 4.77M));
        }

        if (MAL2 < LB2C5 && MAC3 > MAC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.98M)];
            matches!.Add(CreateBasicTrigger(context, 7.98M));
        }

        if (SEMC5 < MAC1 && SEMC1 < UBC1 && MAC2 < LBC5 && SEMC8 < MAC2 && SEMC2 > SEMC5 && SEMC2 > SEMC3 && SEMC2 < MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.68M)];
            matches!.Add(CreateBasicTrigger(context, 4.68M));
        }

        if (SEMC5 < LB2C13 && SEMC5 < LB2C21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.96M)];
            matches!.Add(CreateBasicTrigger(context, 4.96M));
        }

        if (SEMC8 < LBC5 && SEMC13 < LBC8 && SEMC21 < LBC13 && SEMC34 < LBC21 && SEMC8 < SEMC13 && SEMC13 < SEMC21 && SEMC21 < SEMC34)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.28M)];
            matches!.Add(CreateBasicTrigger(context, 7.28M));
        }

        if (UBC5 < LBC21 && SEMC21 < LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.63M)];
            matches!.Add(CreateBasicTrigger(context, 7.63M));
        }

        if (SEMC8 > SEMC13 && (SEMC8 > LBC8 || SEMC13 > LBC8) && SEMC13 < LBC13 && SEMC13 < SEMC21 && SEMC21 < MAC21 && SEMC5 < LBC5 && SEMC5 < LBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.69M)];
            matches!.Add(CreateBasicTrigger(context, 4.69M));
        }

        if (MAC5 < LBC89 && SEMC13 > MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.72M)];
            matches!.Add(CreateBasicTrigger(context, 4.72M));
        }

        if (SEMC13 < MAC3 && SEMC8 < MAC2 && SEMC5 < MAC1 && SEMC5 < LBC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.65M)];
            matches!.Add(CreateBasicTrigger(context, 4.65M));
        }

        if (SEMC5 < LBC5 && UBC1 < SEMC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.57M)];
            matches!.Add(CreateBasicTrigger(context, 4.57M));
        }

        if (SEMC3 < MAC3 && UBC1 < SEMC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.56M)];
            matches!.Add(CreateBasicTrigger(context, 4.56M));
        }

        if (MAC2 > MAC3 && MAC3 > MAC5 && MAC1 > MAC5 && UBC1 < MAC2 && SEMC1 > MAC1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.49M)];
            matches!.Add(CreateBasicTrigger(context, 4.49M));
        }

        if (MAC5 > MAC89 && MAC13 > MAC89 && MAC21 > MAC89 && MAC5 < MAC13 && MAC13 < MAC21 && SEMC21 < LBC21 && SEMC13 < LBC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.59M)];
            matches!.Add(CreateBasicTrigger(context, 4.59M));
        }

        if (SEMC5 < LB2C89 && SEMC13 < LB2C89 && SEMC5 < SEMC13 && SEMC5 < SEMC21 && SEMC8 < SEMC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.42M)];
            matches!.Add(CreateBasicTrigger(context, 4.42M));
        }

        if (MAC2 < MAC3 && MAC3 < MAC5 && MAC1 < MAC5 && SEMC5 < LBC5 && SEMC8 < LBC8 && SEMC13 < LBC13 && UBC1 < MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.41M)];
            matches!.Add(CreateBasicTrigger(context, 4.41M));
        }

        if (UBC5 < MAC8 && MAC5 < MAC8 && MAC8 < MAC13 && MAC21 < LBC89 && MAC5 < LBC89 && MAC8 < LBC89 && SEMC5 > SEMC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.62M)];
            matches!.Add(CreateBasicTrigger(context, 4.62M));
        }

        if (MAL2 > MAC13 && SEMC13 < LBC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.39M)];
            matches!.Add(CreateBasicTrigger(context, 4.39M));
        }

        if (MAL2 > MAC3 && SEMC2 > MAC2 && SEMC3 > MAC3 && (SEMC2 > SEMC3 || SEMC3 > SEMC4) && SEMC2 > SEMC5 && SEMC5 > SEMC8 && SEMC5 > SEMC13 && SEMC5 > SEMC21 && SEMC8 > SEMC13 && (MAL2 > MAC2 || MAL2 > SEMC5) && SEMC21 < LBC21 && SEMC13 < LBC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.54M)];
            matches!.Add(CreateBasicTrigger(context, 4.54M));
        }

        if (SEMC8 < LBC8 && SEMC5 < LBC5 && SEMC3 < LBC3 && SEMC2 < LBC2 && SEMC1 < LBC1 && MAL2 > MAC89 && SEMC13 < LBC13 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.37M)];
            matches!.Add(CreateBasicTrigger(context, 4.37M));
        }

        if (MAC21 < MAC89 && MAC21 > LBC89 && SEMC21 < MAC21 && MAC13 < MAC89 && SEMC13 > MAC13 && SEMC89 < MAC89 && SEMC21 < SEMC89 && SEMC13 > SEMC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.32M)];
            matches!.Add(CreateBasicTrigger(context, 7.32M));
        }

        if (SEMC8 < LBC8 && SEMC5 < LBC5 && SEMC3 < LBC3 && SEMC2 < LBC2 && SEMC1 < LBC1 && MAL2 > MAC89 && SEMC13 < MAC13 && SEMC8 < LBC5 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.53M)];
            matches!.Add(CreateBasicTrigger(context, 4.53M));
        }

        if (SEMC8 > UBC8 && SEMC21 < LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.32M)];
            matches!.Add(CreateBasicTrigger(context, 4.32M));
        }

        if (MAL2 > MAC21 && SEMC21 < LBC21 && SEMC8 < UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.28M)];
            matches!.Add(CreateBasicTrigger(context, 4.28M));
        }

        if (SEMC8 > UBC8 && MAL2 > UBC8 && C5_SEMTrend == 1.1M && C4_SEMTrend == 1.1M && MAL2 > SEMC8 && MAL2 > SEMC5 && MAL2 > SEMC4 && SEMC2 > SEMC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.27M)];
            matches!.Add(CreateBasicTrigger(context, 4.27M));
        }

        if (SEMC8 > UBC8 && C3_SEMTrend != -0.1M && SEMC3 < UBC3 && MAL2 > SEMC8 && MAL2 > SEMC3 && MAL2 > SEMC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.25M)];
            matches!.Add(CreateBasicTrigger(context, 4.25M));
        }

        if (SEMC8 > UBC8 && MAL2 > UBC8 && C3_C13_Flag_Trend_Lower == 1 && C5_C21_Flag_Trend_Lower == -1 && MAC5 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.19M)];
            matches!.Add(CreateBasicTrigger(context, 4.19M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && C5_C21_Flag_Trend_Lower == -1 && MAC3 < MAC13 && SEMC13 < UBC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.33M)];
            matches!.Add(CreateBasicTrigger(context, 4.33M));
        }

        if (SEMC21 < LBC21 && SEMC13 < LBC13 && MAL2 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 4.35M)];
            matches!.Add(CreateBasicTrigger(context, 4.35M));
        }

        if (SEMC21 > UBC21 && MAL2 < LB2C8 && MAL2 < LB2C13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.57M)];
            matches!.Add(CreateBasicTrigger(context, 7.57M));
        }

        if (SEMC21 > UBC21 && SEMC13 < MAC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.65M)];
            matches!.Add(CreateBasicTrigger(context, 7.65M));
        }

        if (SEMC21 > MAC21 && SEMC21 < UBC21 && SEMC13 < LBC13 && MAC21 > MAC89 && MAC13 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.53M)];
            matches!.Add(CreateBasicTrigger(context, 7.53M));
        }

        if (SEMC21 < MAC21 && SEMC21 > LBC21 && SEMC13 < LBC13 && MAC21 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.44M)];
            matches!.Add(CreateBasicTrigger(context, 7.44M));
        }

        if (SEMC21 < LBC21 && SEMC13 > LBC13 && SEMC13 < MAC13 && SEMC13 > SEMC21 && SEMC34 > LBC34)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.46M)];
            matches!.Add(CreateBasicTrigger(context, 7.46M));
        }

        if (SEMC13 > UBC13 && SEMC8 > UBC8 && SEMC21 > UBC21 && C21_UBLB2_Dir == 1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.77M)];
            matches!.Add(CreateBasicTrigger(context, 7.77M));
        }

        if (SEMC21 > UBC21 && SEMC13 > MAC13 && C21_UBLB2_Dir == 1)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.76M)];
            matches!.Add(CreateBasicTrigger(context, 7.76M));
        }

        if (SEMC5 > UBC5 && SEMC8 < UBC8 && SEMC8 > MAC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && SEMC21 < MAC21 && SEMC21 > LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.23M)];
            matches!.Add(CreateBasicTrigger(context, 7.23M));
        }

        if (SEMC5 < UBC5 && SEMC5 > MAC5 && SEMC8 < UBC8 && SEMC8 > MAC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && SEMC21 < MAC21 && SEMC21 > LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.21M)];
            matches!.Add(CreateBasicTrigger(context, 7.21M));
        }

        if (SEMC5 < UBC5 && SEMC5 > MAC5 && SEMC8 > UBC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && SEMC21 < MAC21 && SEMC21 > LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.18M)];
            matches!.Add(CreateBasicTrigger(context, 7.18M));
        }

        if (SEMC5 > UBC5 && SEMC8 > UBC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && SEMC21 < MAC21 && SEMC21 > LBC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.22M)];
            matches!.Add(CreateBasicTrigger(context, 7.22M));
        }

        if (SEMC5 > UBC5 && SEMC1 < LBC1 && SEMC2 > UBC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.11M)];
            matches!.Add(CreateBasicTrigger(context, 7.11M));
        }

        if (SEMC13 > MAC13 && SEMC13 < UBC13 && SEMC21 > MAC21 && SEMC21 < UBC21 && C13_SEMTrend != -0.1M && SEMC13 > SEMC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.34M)];
            matches!.Add(CreateBasicTrigger(context, 7.34M));
        }

        if (LB2C1 > UBC4)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.04M)];
            matches!.Add(CreateBasicTrigger(context, 7.04M));
        }

        if (SEMC5 > UBC5 && SEMC8 > MAC8 && SEMC8 < UBC8 && SEMC3 > SEMC8 && C8_SEMTrend != -0.1M)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.14M)];
            matches!.Add(CreateBasicTrigger(context, 7.14M));
        }

        if (MAC21 < MAC89 && MAC21 > LBC89 && SEMC21 < MAC21 && SEMC89 < MAC89 && SEMC5 > LBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.17M)];
            matches!.Add(CreateBasicTrigger(context, 7.17M));
        }

        if (SEMC5 > MAC5 && SEMC5 < UBC5 && SEMC8 > MAC8 && SEMC8 < UBC8 && SEMC5 >= SEMC8 && C8_SEMTrend != -0.1M && C5_SEMTrend != -0.1M)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.12M)];
            matches!.Add(CreateBasicTrigger(context, 7.12M));
        }

        if (MAC2 > UBC8 && MAL2 < LBC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.25M)];
            matches!.Add(CreateBasicTrigger(context, 7.25M));
        }

        if (MAL2 > LB2C5 && MAL2 < LB2C8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.56M)];
            matches!.Add(CreateBasicTrigger(context, 6.56M));
        }

        if (MAC5 > UBC21 && SEMC21 > UBC21 && SEMC5 < MAC5 && SEMC8 > MAC8 && SEMC13 > MAC13)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.65M)];
            matches!.Add(CreateBasicTrigger(context, 6.65M));
        }

        if (LB2C2 > MAC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.52M)];
            matches!.Add(CreateBasicTrigger(context, 6.52M));
        }

        if (C8_SEMTrend != -0.1M && SEMC8 > SEMC13 && MAL2 > SEMC8 && MAL2 > UBC8 && SEMC8 > UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.79M)];
            matches!.Add(CreateBasicTrigger(context, 6.79M));
        }

        if (MAC2 > UBC5 && MAC3 > UBC8)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.83M)];
            matches!.Add(CreateBasicTrigger(context, 7.83M));
        }

        if (SEMC5 < LBC5 && SEMC8 < MAC8 && SEMC13 < UBC13 && SEMC13 > MAC13 && C13_SEMTrend == -0.1M && SEMC21 > UBC21 && C13_SEMTrend == -0.1M && SEMC13 < SEMC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.48M)];
            matches!.Add(CreateBasicTrigger(context, 6.48M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC5 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.47M)];
            matches!.Add(CreateBasicTrigger(context, 6.47M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC3 < MAC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.46M)];
            matches!.Add(CreateBasicTrigger(context, 6.46M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC2 < MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.45M)];
            matches!.Add(CreateBasicTrigger(context, 6.45M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC1 < MAC1 && MAC3 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.44M)];
            matches!.Add(CreateBasicTrigger(context, 6.44M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC1 < MAC1 && MAL2 < MAC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.43M)];
            matches!.Add(CreateBasicTrigger(context, 6.43M));
        }

        if (C3_C13_Flag_Trend_Lower == 1 && MAC1 > MAC3 && MAC2 > MAC3 && MAC1 > MAC2 && C5_Pivot_Status != -2 && SEMC1 < MAC1 && MAL2 < MAC3)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.42M)];
            matches!.Add(CreateBasicTrigger(context, 6.42M));
        }

        if (SEMC5 < LBC5 && SEMC8 < LBC8 && SEMC13 < UBC13 && SEMC13 > LBC13 && SEMC21 > MAC21 && C21_SEMTrend == -0.1M && SEMC13 < SEMC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.41M)];
            matches!.Add(CreateBasicTrigger(context, 6.41M));
        }

        if (LB2C1 > MAC2)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.51M)];
            matches!.Add(CreateBasicTrigger(context, 6.51M));
        }

        if (SEMC21 < LBC21 // 7.41M Done 
            && SEMC89 > MAC89 && MAC21 > MAC89
            && MAL2 <= MAC89 && MAC8 < MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 7.41M)];
            matches!.Add(CreateBasicTrigger(context, 7.41M));
        }

        if (MAC13 < MAC21 && SEMC21 < MAC21 && SEMC21 > LBC21  // 6.27M Done 
            && SEMC13 < MAC13 && SEMC13 > LBC13
            && SEMC8 < MAC8 && SEMC8 > LBC8
            && SEMC13 < SEMC21 && SEMC8 < SEMC21
            && MAL2 < MAC5 && MAL2 > LBC5)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.27M)];
            matches!.Add(CreateBasicTrigger(context, 6.27M));
        }

        if (MAC13 > MAC21 && MAC21 > MAC34 // 6.29M done 
            && (LB2C13 > MAC34 || LBC21 > MAC34)
            && SEMC13 < MAC13 && SEMC21 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.29M)];
            matches!.Add(CreateBasicTrigger(context, 6.29M));
        }


        if (SEMC34 < MAC34 && SEMC21 < MAC21 && SEMC13 < MAC13  // 6.28M done 
            && SEMC34 > MAC89 && SEMC21 > MAC89 && SEMC13 > MAC89
            && SEMC5 > MAC89 && MAC1 > MAC89
            && LB2C34 > MAC89)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 6.28M)];
            matches!.Add(CreateBasicTrigger(context, 6.28M));
        }

        /****************************************still not finished ****************************************************/
        /****************************************still not finished ****************************************************/
        /****************************************still not finished ****************************************************/

        //if (MAC8 < LB2C89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.31M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.31M));
        //}

        //if (MAC13 < MAC21
        //    && MAC5 > MAC13 && MAC5 < MAC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.33M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.33M));
        //}

        //if (MAC13 > MAC21 && MAC8 > MAC21
        //    && SEMC5 > MAC5 && SEMC8 < MAC8
        //    && SEMC13 > MAC13 && SEMC21 > MAC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.25M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.25M));
        //}

        //if (SEMC21 < MAC21
        //    && (SEMC89 < MAC89 || MAC21 < MAC89)
        //    && MAL2 <= MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.34M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.34M));
        //}

        //if (SEMC21 < MAC21
        //     && SEMC89 < MAC89 && MAC21 < MAC89
        //     && MAL2 <= MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.33M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.33M));
        //}

        //if (SEMC21 > MAC21
        //    && (SEMC89 < MAC89 || MAC21 < MAC89)
        //    && MAL2 <= MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.32M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.32M));
        //}

        //if (SEMC21 < LBC21 /*&& SEMC34 > LBC34*/ && SEMC34 < MAC34
        //    && SEMC55 > MAC55 && SEMC89 > MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.34M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.34M));
        //}

        //if (SEMC21 < LBC21 && SEMC34 < MAC34
        //    && SEMC55 < MAC55 && SEMC89 > MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.33M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.33M));
        //}

        //if (UBC13 < MAC21 /*|| UB2C13 < UBC21*/)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.12M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.12M));
        //}

        //if (MAL2 > UB2C13 && MAL2 > UB2C21 && MAC1 <= UB2C8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.39M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.39M));
        //}

        //if (SEMC21 < LBC21 && SEMC13 > LBC13 && SEMC13 < MAC13
        //    && SEMC13 > SEMC21 && UBC13 < MAC21 && SEMC34 <= LBC34)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.37M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.37M));
        //}

        //if (  // later needed 0 trades   
        //    SEMC21 < LBC21 /*&& SEMC13 < MAC13 */
        //    && SEMC34 < LBC34)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.39M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.39M));
        //}

        //if (MAC3 > UB2C21)    // later  
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.07M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.07M));
        //}

        //if (MAC5 < MAC89 && MAC21 < MAC89 && SEMC89 < LBC89
        //    && SEMC13 < SEMC21 && SEMC8 < SEMC21
        //    && SEMC21 > SEMC89
        //    /*&& SEMC13 < MAC13*/)
        //{   // 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.58M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.58M));
        //}

        //if (MAC5 < LBC89 && SEMC89 < LBC89 && SEMC21 > SEMC89) // later
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.64M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.64M));
        // }

        //if (SEMC8 < LBC8 && SEMC5 < LBC5 && SEMC3 < LBC3
        //    && SEMC2 < LBC2 && SEMC1 < LBC1
        //    && SEMC89 < LBC89)
        //{
        //    //this location is 0 trades  all long trades are in different number but the longs are strong tested as first priority but keep it later
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.38M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.38M));
        // }

        //////////////should be done
        ////////////if (MAC5 > UBC13 && MAC8 > UBC21 
        ////////////    && SEMC5 > MAC5
        ////////////    && SEMC8 > UBC8 && SEMC13 > UBC13 && SEMC21 > UBC21)
        ////////////{
        ////////////    // should adjust when done 6.5199M block
        ////////////    if (!getAll)
        ////////////        return [CreateBasicTrigger(context, 6.49M)];
        ////////////    matches!.Add(CreateBasicTrigger(context, 6.49M));
        //////////// }


        //////////if (SEMC21 > UBC21 && SEMC13 > UBC13
        //////////    && MAC8 > UBC21 && SEMC8 > UBC21
        //////////    && C5_SEMTrend != -0.1M && SEMC5 > MAC5)
        //////////{
        //////////    if (!getAll)
        //////////        return [CreateBasicTrigger(context, 6.55M)];
        //////////    matches!.Add(CreateBasicTrigger(context, 6.55M));
        //////////}

        //////////if (SEMC5 > UBC5 && C5_SEMTrend == -0.1M
        //////////    && SEMC4 > UBC4 && C4_SEMTrend == -0.1M
        //////////    && SEMC3 > UBC3 && C3_SEMTrend == -0.1M
        //////////    && SEMC2 > UBC2 && C2_SEMTrend == -0.1M
        //////////    && SEMC1 > UBC1 && C1_SEMTrend == -0.1M)
        //////////{
        //////////    if (!getAll)
        //////////        return [CreateBasicTrigger(context, 6.54M)];
        //////////    matches!.Add(CreateBasicTrigger(context, 6.54M));
        //////////}

        //////////if ( SEMC5 > UBC5 && C5_SEMTrend == -0.1M
        //////////    && SEMC4 > UBC4 && C4_SEMTrend == -0.1M
        //////////    && SEMC3 > UBC3 && C3_SEMTrend == -0.1M
        //////////    && SEMC2 > UBC2 && C2_SEMTrend == -0.1M)
        //////////{
        //////////    if (!getAll)
        //////////        return [CreateBasicTrigger(context, 6.53M)];
        //////////    matches!.Add(CreateBasicTrigger(context, 6.53M));
        //////////}

        //if (MAC8 > UBC21 && MAC5 > UBC21
        //    && SEMC8 > UBC8 && SEMC5 > UBC5
        //    && SEMC21 > UBC21 && C21_SEMTrend != -0.1M
        //    && SEMC3 < MAC3 && SEMC13 > UBC13
        //    && MAL2 > MAC3)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.58M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.58M));
        //}

        //////if (C3_SEMTrend == 0 && (MAC2 < MAC3 || MAC2 < MAC5))
        //////{
        //////    if (!getAll)
        //////        return [CreateBasicTrigger(context, 6.57M)];
        //////    matches!.Add(CreateBasicTrigger(context, 6.57M));
        //////}

        //if (SEMC21 > UBC21 && SEMC13 > MAC13
        //    && SEMC8 < UBC8
        //    && (C8_SEMTrend != -0.1M || SEMC8 < MAC8)
        //    && SEMC5 > SEMC8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.75M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.75M));
        //}

        //if (LB2C1 > Math.Max(UB2C13, UB2C21))
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.88M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.88M));
        // }

        //if (MAC1 > UB2C21 && SEMC21 < UBC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.03M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.03M));
        //}

        //if (SEMC21 < UBC13 && SEMC8 < UBC13 && SEMC5 < UBC13
        //    && SEMC4 < UBC13 && SEMC3 < UBC13 && SEMC2 < UBC13
        //    && SEMC1 < UBC13 && SEMC13 > UBC13)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.09M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.09M));
        //}

        //if (C5_SEMTrend != -0.1M && C8_SEMTrend == -0.1M
        //    && SEMC5 > SEMC8 && SEMC5 > SEMC13
        //    && MAL2 > SEMC5 && MAL2 > UBC5
        //    && SEMC5 > UBC5)   // should stay here after 7.14M 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.78M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.78M));
        //}

        //if (SEMC3 > UB2C8 && SEMC2 > UB2C8
        //    && SEMC5 <= UB2C8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.02M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.02M));
        //}

        //if (SEMC1 > UBC1
        //    && SEMC5 >= UBC5
        //    && UBC1 < SEMC5 && UB2C1 > SEMC5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.71M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.71M));
        //}

        //if (SEMC21 > UBC21 && SEMC13 > UBC13
        //    && SEMC8 < MAC8) // C8 Series 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.64M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.64M));
        //}

        //if (C5_SEMTrend != -0.1M && C8_SEMTrend == -0.1M
        //    && SEMC5 > SEMC8 && SEMC5 > SEMC13
        //    && MAL2 > SEMC5 && MAL2 > UBC5
        //    && SEMC5 > UBC5) // not much need from up test 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.78M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.78M));
        //}

        //if (SEMC13 > MAC13 && SEMC13 > UBC13
        //    && SEMC21 > MAC21
        //    && C13_SEMTrend != -0.1M && SEMC13 > SEMC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.33M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.33M));
        //}

        //if (SEMC13 > MAC13
        //    && SEMC21 > LBC21 && SEMC21 < UBC21
        //    && (SEMC21 < SEMC89 || SEMC21 < SEMC55)
        //    && MAC21 > MAC89
        //    && SEMC13 > SEMC21
        //    && SEMC89 < UBC21 && SEMC55 < UBC21 && SEMC34 < UBC21
        //    && SEMC89 > MAC21 && SEMC55 > MAC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.07M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.07M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > UBC8
        //    && SEMC21 > MAC21 && SEMC21 < UBC21
        //    && C21_UBLB2_Dir == -1)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.09M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.09M));
        //}

        //if (SEMC13 > UBC13 && MAL2 > MAC21
        //    && SEMC21 > MAC21 && SEMC21 < UBC21
        //    && MAC5 > MAC21 && SEMC5 > MAC21
        //    && SEMC5 < MAC5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.63M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.63M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > UBC8
        //    && SEMC21 > MAC21 && SEMC21 < UBC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.62M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.62M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > MAC8
        //    && SEMC21 > MAC21 && SEMC21 < UBC21 && C21_SEMTrend != -0.1M
        //    && MAC5 > MAC21 && MAC5 > MAC13
        //    && SEMC5 > UBC5 && C5_SEMTrend != -0.1M
        //    && MAL2 > MAC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.61M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.61M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > UBC8
        //    && SEMC21 > UBC21 && C21_SEMTrend != -0.1M
        //    && MAC5 > UBC21 && MAC3 > UBC8 && MAC2 > UBC8
        //    && SEMC5 > UBC5 && C5_SEMTrend == -0.1M
        //    && MAL2 > MAC5 && MAL2 < UBC5 && MAC1 < UBC5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.59M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.59M));
        //}

        //if (SEMC1 > UBC1
        //    && SEMC5 < MAC5
        //    && UBC1 < SEMC5 && UB2C1 > SEMC5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 6.69M)];
        //    matches!.Add(CreateBasicTrigger(context, 6.69M));
        //}

        //if (MAC21 < MAC89 && MAC21 > LBC89 && SEMC21 < MAC21
        //    && SEMC89 < MAC89
        //    && SEMC5 <= LBC5) // only on trade 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.09M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.09M));
        //}

        //if (SEMC21 > MAC21 && SEMC21 < UBC21
        //    && SEMC13 < MAC13 && SEMC13 >= LBC13)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.56M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.56M));
        //}

        //if (MAC5 < MAC89 && MAC5 > LBC89
        //    && SEMC89 <= MAC89)
        //{   // 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.44M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.44M));
        //}

        //if (C5_Pivot_Status == -2 && C21_Pivot_Status == -2 && MAC1 < MAC3
        //    /*&& (C2_MAC_Pivot_Trend == 1 || C5_MAC_Pivot_Trend == 1)*/ )
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 1.88M)];
        //    matches!.Add(CreateBasicTrigger(context, 1.88M));
        //}

        //if (SEMC21 > UBC21
        //    && SEMC1 < UBC21 && SEMC2 < UBC21 && SEMC3 < UBC21
        //    && SEMC4 < UBC21 && SEMC5 < UBC21 && SEMC8 < UBC21 && SEMC13 < UBC21) // later
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 2.33M)];
        //    matches!.Add(CreateBasicTrigger(context, 2.33M));
        //}

        //if (SEMC8 > UB2C13 && SEMC8 > UB2C21
        //    && SEMC5 > UB2C13 && SEMC5 < SEMC8) // should be done 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.27M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.27M));
        //}

        //////if (C21_Pivot_Status == -2 && MAC1 < MAC3
        //////    && (C2_MAC_Pivot_Trend == 1 || C5_MAC_Pivot_Trend == 1))
        //////{
        //////    if (!getAll)
        //////        return [CreateBasicTrigger(context, 7.31M)];
        //////    matches!.Add(CreateBasicTrigger(context, 7.31M));
        //////}

        //if (SEMC1 > UB2C4 && SEMC1 > UB2C5
        //    && SEMC5 > UBC5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.68M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.68M));
        //}

        //if (SEMC2 > UB2C5 && SEMC1 < UB2C5 && SEMC3 < UB2C5
        //    && SEMC1 < SEMC2)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.67M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.67M));
        //}

        //if (SEMC2 > UB2C5 && SEMC1 < UB2C5 && SEMC3 > UB2C5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.66M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.66M));
        //}

        //if (SEMC5 < MAC5 && SEMC5 > LBC5 && SEMC8 > UBC8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.04M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.04M));
        //}

        //if (SEMC5 < MAC5 && SEMC5 > LBC5 && SEMC8 > UBC8
        //    && MAC5 > MAC8 && MAC8 > MAC13 && MAC13 > MAC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.04M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.04M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > UBC8
        //    && SEMC21 > MAC21 && SEMC21 < UBC21
        //    && C21_UBLB2_Dir == 1)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.11M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.11M));
        //}

        //if (SEMC21 < MAC21 && SEMC21 > LBC21
        //    && cParam.C21_SEMLB1LB2_Stage == 1
        //    && SEMC13 < MAC13) // try it  
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.39M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.39M));
        //}

        //if (SEMC13 > MAC13 && SEMC8 > UBC8
        //    && SEMC21 > UBC21
        //    && SEMC5 < UBC5 && C5_SEMTrend != -0.1M
        //    && C21_UBLB2_Dir == -1)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.71M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.71M));
        //}

        //if (C3_C13_S_Cont_LB1 < 0
        //     && SEMC13 > UBC13 && SEMC8 > UBC8
        //     && SEMC8 < SEMC13)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.73M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.73M));
        //}

        //if ((SEMC5 > Math.Max(UB2C13, UB2C21) || SEMC8 > Math.Max(UB2C13, UB2C21))
        //    && SEMC5 < SEMC8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.72M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.72M));
        //}

        //if (UBC1 < LBC4) // test it it is in 4.71M 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.16M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.16M));
        //}

        //if (SEMC13 > UBC13 && SEMC8 > UBC8
        //    && SEMC21 < MAC21 && SEMC21 > LBC21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.13M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.13M));
        //}

        //if (MAC1 > UB2C21 || MAC2 > UB2C21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.15M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.15M));
        //}

        //if (C5_Pivot_Status == -2)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.23M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.23M));
        //}

        //if (MAL2 < UB2C21 && SEMC3 > UB2C21 && SEMC2 > UB2C21
        //    && SEMC4 > UB2C21 && SEMC5 > UB2C21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.21M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.21M));
        //}

        //if (SEMC8 > UB2C13)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.17M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.17M));
        //}

        //if (SEMC13 > UB2C21)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.24M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.24M));
        //}

        //if (// && SEMC8 > UBC8 /*&& MAL2 > UBC8*/
        //    MAC2 > UBC8 && MAC1 > UBC8
        //    && SEMC1 < MAC1 && SEMC1 > UBC5)    // should do next and should be before 4.19M 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.18M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.18M));
        //}

        //if (SEMC8 > UBC8 && SEMC13 < UBC13 && SEMC21 < MAC21) // should do next and should be before 4.19M 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.26M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.26M));
        //}

        //if (C3_C13_Flag_Trend_Lower == 1 && C5_C21_Flag_Trend_Lower == -1
        //    && MAC5 > MAC89)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.22M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.22M));
        //}

        //if (LBC1 > UBC3)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.85M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.85M));
        //}

        //if (SEMC89 < LBC89 && MAC5 < LBC89 /*&& SEMC13 < MAC5*/ && MAC5 < MAC21
        //    && SEMC21 > SEMC89 && MAC21 < LBC89 /*&& SEMC5 < MAC5*/ ) // later 
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.63M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.63M));
        //}

        //if (MAC5 < LBC89 && SEMC13 < MAC5 && MAC5 < MAC21
        //    && SEMC21 > LBC21 /*&& MAC21 < LBC89 && SEMC5 < MAC5*/ ) // later
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.66M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.66M));
        //}

        //if (SEMC8 < LBC8 && SEMC8 < LBC13 && MAC3 < LBC13
        //    && SEMC5 < LBC5 && SEMC5 < LBC8
        //    && SEMC5 < LBC13 && MAC5 < LBC21
        //    && SEMC13 > LBC13 && SEMC21 > MAC21)
        //{   // 0 trades here 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.61M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.61M));
        //}

        //if (MAC5 < LBC89 && MAC5 > LB2C89
        //    && ((MAC13 < MAC89 && MAC13 > LBC89)
        //    || (MAC13 < MAC89 && MAC13 > LBC89)))
        //{   // 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.61M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.61M));
        //}

        //if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1
        //    && MAL2 > MAC1 && MAL2 > SEMC1)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.55M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.55M));
        //}

        //if (MAC5 < MAC89 && MAC5 > LBC89
        //    && MAC21 > MAC89 && MAC13 > MAC89)
        //{   // 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.46M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.46M));
        //}

        //if (MAC5 < MAC89 && MAC5 > LBC89
        //    && SEMC89 < LBC89)
        //{   // 
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.45M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.45M));
        //}

        //if (SEMC1 < MAC1 && MAL2 < MAC1 && MAL2 < SEMC1 /*&& SEMC1 < MAC5*/ ) // work on 	
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 7.87M)];
        //    matches!.Add(CreateBasicTrigger(context, 7.87M));
        //}

        //if (C5_Pivot_Status == -2 && C21_Pivot_Status == -2 /*&& MAC1 < MAC3*/ )
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 1.93M)];
        //    matches!.Add(CreateBasicTrigger(context, 1.93M));
        //}

        //if (/*&& MAC1 > SEMC3 && MAC1 > SEMC2*/ 
        //    MAC2 <= LBC13
        //    && (/*pStudy.C3_C5_LLB2_Cont_MAC == 1 ||*/ C3_C5_LLB2_Cont_MAC == 1))
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.89M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.89M));
        //}

        //if (SEMC3 <= Math.Min(LB2C13, LB2C21)
        //    && MAC2 < LBC13
        //    && SEMC2 > LB2C5)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.61M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.61M));
        //}

        //if (SEMC5 < LB2C8)
        //{
        //    if (!getAll)
        //        return [CreateBasicTrigger(context, 4.97M)];
        //    matches!.Add(CreateBasicTrigger(context, 4.97M));
        //}

        //if (SEMC2 < LB2C4 && SEMC1 > MAC1) // 8115 21 34 trades up 
        //{
        //    if (MAC2 <= Math.Min(LB2C13, LB2C21)) // 3 -73400 
        //    {
        //        if (SEMC2 < LB2C4) // 2267300 273  
        //        {
        //            if (!getAll)
        //                return [CreateBasicTrigger(context, 4.95M)];
        //            matches!.Add(CreateBasicTrigger(context, 4.95M));
        //        }
        //    }
        //}
        /****************************************still not finished ****************************************************/
        /****************************************still not finished ****************************************************/
        /****************************************still not finished ****************************************************/

        if (MAL2 > MAC21)
        {
            if (!getAll)
                return [CreateBasicTrigger(context, 9.16M)];
            matches!.Add(CreateBasicTrigger(context, 9.16M));
        }

        return matches is { Count: > 0 } ? matches : [CreateBasicTrigger(context, 9.15M)];
        decimal ResolveBasicTrigger498()
        {
            if (SEMC5 < LB2C8 && SEMC3 < LB2C5 && SEMC2 < LB2C4 && SEMC1 < LB2C3)
                return 4.98M;
            else if (SEMC3 > Math.Min(LB2C13, LB2C21) && MAL2 > LB2C5 && MAC1 > SEMC4 && MAL2 > MAC1 && MAL1 > MAC1 && MAL3 > MAL2 && MAL2 > MAL1 && SEMC1 > MAC1 && SEMC1 < UBC1)
                return 4.98M;
            else if (SEMC3 <= Math.Min(LB2C13, LB2C21) && MAL2 > LB2C5 && MAC1 > SEMC4 && MAC1 > SEMC5 && SEMC2 < LB2C5)
                return 4.98M;
            else if (MAL2 > LBC2 && SEMC1 > MAC1 && MAL3 < MAC1 && MAL2 < MAC1 && MAL1 < MAC1 && MAL3 < MAL2 && MAL2 < MAL1 && SEMC1 < UBC1)
                return 4.98M;
            else if (SEMC5 < LB2C8)
            {
                if ((MAC2 < LBC13 || MAC2 < LBC5) && MAL2 < Math.Max(SEMC5, LB2C8))
                    return 4.98M;
                else if (C5_Pivot_Status_Odd > 0)
                {
                    return 7.99M;
                }
                else
                    return 4.98M;
            }
            else if (SEMC2 > LB2C5)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 < LBC13 && SEMC1 > MAC1 && MAL2 < SEMC1 && MAL2 < Math.Max(SEMC5, MAC2))
                        return 4.98M;
                    else if (MAC2 < LBC13 && SEMC1 < MAC1 && MAL2 < Math.Max(SEMC5, MAC2) && MAL3 < MAC1 && MAL2 < MAC1 && MAL1 < MAC1)
                        return 4.98M;
                    else if (C5_Pivot_Status_Odd > 0)
                    {
                        return 7.99M;
                    }
                    else
                        return 4.98M;
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 > LBC13 && pC2_C5_LLB2_Cont_MAC == 0 && MAL2 < Math.Max(SEMC5, MAC2))
                        return 4.98M;
                    else if (MAC2 < LBC13 && pC2_C5_LLB2_Cont_MAC == 0 && MAL2 < Math.Max(SEMC5, MAC2))
                        return 4.98M;
                    else if (MAC2 < LBC13 && pC2_C5_LLB2_Cont_MAC == 1 && MAL2 < Math.Max(SEMC5, MAC2))
                        return 4.98M;
                    else if (C5_Pivot_Status_Odd > 0)
                    {
                        return 7.99M;
                    }
                    else
                        return 4.98M;
                }
            }
            else if (SEMC1 > MAC1)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 < LBC13 && MAL2 < SEMC1 && MAL2 < Math.Max(SEMC5, MAC2))
                        return 4.98M;
                    else if (C5_Pivot_Status_Odd > 0)
                    {
                        return 7.99M;
                    }
                    else
                        return 4.98M;
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAL2 > SEMC4 && MAC1 > SEMC4 && MAL1 > MAC1 && MAL2 > MAC1 && MAL3 < UBC1)
                        return 4.98M;
                    else if (MAL2 > SEMC4 && MAL3 > MAL1 && MAL3 > MAL2 && SEMC1 < SEMC5 && SEMC2 > LB2C4 && SEMC2 < LB2C5)
                        return 4.98M;
                    else if (MAC1 < SEMC4)
                        return 4.98M;
                    else if (C5_Pivot_Status_Odd > 0)
                    {
                        return 7.99M;
                    }
                    else
                        return 4.98M;
                }
            }
            else if (MAL2 < LB2C5)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 > LBC5)
                    {
                        if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && (MAL1 > LB2C5 || MAL2 > LB2C4))
                            return 4.98M;
                        else if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && MAL1 < LB2C5 && MAL2 < LB2C4 && MAL3 > LB2C2 && SEMC1 < LB2C2)
                            return 4.98M;
                        else if (SEMC5 > SEMC8 || SEMC4 > SEMC5)
                            return 4.98M;
                        else if (C5_Pivot_Status_Odd > 0)
                        {
                            return 7.99M;
                        }
                        else
                            return 4.98M;
                    }
                    else if (MAC2 <= LBC5)
                    {
                        if (MAL2 < SEMC4 && LB2C5 > LB2C8)
                            return 4.98M;
                        else if (C5_Pivot_Status_Odd > 0)
                        {
                            return 7.99M;
                        }
                        else
                            return 4.98M;
                    }
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAL1 < LB2C5 && MAL3 > MAL1)
                        return 4.98M;
                    else if (SEMC5 > LBC13 && MAC2 > LBC8 && MAC5 > MAC8 && C13_UBLB2_Dir == 1)
                        return 4.98M;
                    else if (MAC1 < LB2C8 && MAL2 < SEMC4 && UBC1 > SEMC4)
                        return 4.98M;
                    else if (MAL2 < SEMC4 && UBC1 > SEMC4)
                        return 4.98M;
                    else if (C5_Pivot_Status_Odd > 0)
                    {
                        return 7.99M;
                    }
                    else
                        return 4.98M;
                }
            }
            else if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                return 4.98M;
            else if (SEMC3 > Math.Min(LB2C13, LB2C21))
            {
                if (C5_Pivot_Status_Odd > 0)
                {
                    return 7.99M;
                }
                else
                    return 4.98M;
            }

            return 4.98M;
        }
    }

    public override L2Trigger GetTriggerValue(L2TriggerContext context, decimal basicTrigger)
    {
        var cClose = context.cClose;
        var cHigh = context.cHigh;
        var MAC1 = context.MAC1;
        var MAC13 = context.MAC13;
        var MAC2 = context.MAC2;
        var MAC21 = context.MAC21;
        var MAC3 = context.MAC3;
        var MAC4 = context.MAC4;
        var MAC5 = context.MAC5;
        var MAC8 = context.MAC8;
        var MAC34 = context.MAC34;
        var MAC55 = context.MAC55;
        var MAC89 = context.MAC89;
        var MAL1 = context.MAL1;
        var MAL2 = context.MAL2;
        var MAL3 = context.MAL3;
        var SEMC1 = context.SEMC1;
        var SEMC13 = context.SEMC13;
        var SEMC34 = context.SEMC34;
        var SEMC2 = context.SEMC2;
        var SEMC21 = context.SEMC21;
        var SEMC3 = context.SEMC3;
        var SEMC4 = context.SEMC4;
        var SEMC5 = context.SEMC5;
        var SEMC8 = context.SEMC8;
        var SEML1 = context.SEML1;
        var SEML2 = context.SEML2;
        var SEML3 = context.SEML3;
        var SEMC55 = context.SEMC55;
        var SEMC89 = context.SEMC89;
        var LB2C13 = context.LB2C13;
        var LB2C21 = context.LB2C21;
        var LB2C3 = context.LB2C3;
        var LB2C4 = context.LB2C4;
        var LB2C5 = context.LB2C5;
        var LB2C8 = context.LB2C8;
        var LB2C89 = context.LB2C89;
        var UBC1 = context.UBC1;
        var UBL1 = context.UBL1;
        var LBC1 = context.LBC1;
        var LBC13 = context.LBC13;
        var LBC34 = context.LBC34;
        var LBC89 = context.LBC89;
        var LBC2 = context.LBC2;
        var LBC21 = context.LBC21;
        var LBC3 = context.LBC3;
        var LBC4 = context.LBC4;
        var LBC5 = context.LBC5;
        var LBC8 = context.LBC8;
        var UB2C1 = context.UB2C1;
        var UB2C2 = context.UB2C2;
        var UB2C5 = context.UB2C5;
        var UBC13 = context.UBC13;
        var UBC2 = context.UBC2;
        var UBC3 = context.UBC3;
        var UBC4 = context.UBC4;
        var UBC5 = context.UBC5;
        var UBC7 = context.UBC7;
        var UBC8 = context.UBC8;
        var UBC9 = context.UBC9;
        var UBH2 = context.UBH2;
        var UBC21 = context.UBC21;
        var UBL2 = context.UBL2;
        var UB2C13 = context.UB2C13;
        var UB2C21 = context.UB2C21;
        var UB2C3 = context.UB2C3;
        var UB2C8 = context.UB2C8;
        var UB2C4 = context.UB2C4;
        var C13_SEMTrend = context.C13_SEMTrend;
        var C3_SEMTrend = context.C3_SEMTrend;
        var C1_C2_S_Cont_MAC = context.C1_C2_S_Cont_MAC;
        var C1_C3_S_Cont_MAC = context.C1_C3_S_Cont_MAC;
        var L1_C3_S_cont_MAC = context.L1_C3_S_cont_MAC;
        var C1_C5_Conf_SEMs = context.C1_C5_Conf_SEMs;
        var C1_C5_LLB2_Cont_UB1 = context.C1_C5_LLB2_Cont_UB1;
        var C1_C5_S_Cont_LB1 = context.C1_C5_S_Cont_LB1;
        var C3_C13_S_Cont_LB1 = context.C3_C13_S_Cont_LB1;
        var C1_MAC_Pivot_Trend = context.C1_MAC_Pivot_Trend;
        var C1_SEMTrend = context.C1_SEMTrend;
        var C21_MajorTrend = context.C21_MajorTrend;
        var C21_Pivot_Status = context.C21_Pivot_Status;
        var C13_Pivot_Status = context.C13_Pivot_Status;
        var C2_C3_S_Cont_MAC = context.C2_C3_S_Cont_MAC;
        var C2_C4_S_Cont_MAC = context.C2_C4_S_Cont_MAC;
        var C2_C5_Conf_MAs = context.C2_C5_Conf_MAs;
        var C2_C8_L_Cont_UB2 = context.C2_C8_L_Cont_UB2;
        var C2_MAC_Pivot_Trend = context.C2_MAC_Pivot_Trend;
        var C2_Pivot_Status_Long = context.C2_Pivot_Status_Long;
        var C2_Pivot_Status_Odd = context.C2_Pivot_Status_Odd;
        var C2_SEMShortTrend = context.C2_SEMShortTrend;
        var C2_SEMTrend = context.C2_SEMTrend;
        var C3_C13_Flag_Trend_Lower = context.C3_C13_Flag_Trend_Lower;
        var C3_C5_L_Cont_UB1 = context.C3_C5_L_Cont_UB1;
        var C3_C5_S_Cont_LB1 = context.C3_C5_S_Cont_LB1;
        var C3_C5_S_Cont_MAC = context.C3_C5_S_Cont_MAC;
        var C3_C8_L_Cont_UB2 = context.C3_C8_L_Cont_UB2;
        var C3_C8_S_Cont_LB1 = context.C3_C8_S_Cont_LB1;
        var C3_SEMShortTrend = context.C3_SEMShortTrend;
        var C5_C13_L_Cont_UB2 = context.C5_C13_L_Cont_UB2;
        var C5_C21_Flag_Trend_Lower = context.C5_C21_Flag_Trend_Lower;
        var C5_C8_S_Cont_MAC = context.C5_C8_S_Cont_MAC;
        var C2_C8_MAS_LB1C_Cont_MAC = context.C2_C8_MAS_LB1C_Cont_MAC;
        var C5_MAC_Pivot_Trend = context.C5_MAC_Pivot_Trend;
        var C5_Pivot_Status = context.C5_Pivot_Status;
        var C5_Pivot_Status_Long = context.C5_Pivot_Status_Long;
        var C5_Pivot_Status_Odd = context.C5_Pivot_Status_Odd;
        var C5_SEMShortTrend = context.C5_SEMShortTrend;
        var C5_SEMTrend = context.C5_SEMTrend;
        var C4_SEMTrend = context.C4_SEMTrend;
        var L1_C1_S_Cont_LB1 = context.L1_C1_S_Cont_LB1;
        var L1_C2_S_Cont_LB1 = context.L1_C2_S_Cont_LB1;
        var L1_C2_S_Cont_MAC = context.L1_C2_S_Cont_MAC;
        var L1_C2_S_Cont_SEM = context.L1_C2_S_Cont_SEM;
        var L2_MAC_Pivot_Trend = context.L2_MAC_Pivot_Trend;
        var C2_MAC_SupRes = context.C2_MAC_SupRes;
        var C5_MAC_SupRes = context.C5_MAC_SupRes;
        var L2_MAC_SupRes = context.L2_MAC_SupRes;
        var C8_SEMTrend = context.C8_SEMTrend;
        var C1_UBLB2_Dir = context.C1_UBLB2_Dir;
        var C21_UBLB1_Dir = context.C21_UBLB1_Dir;
        var C21_UBLB2_Dir = context.C21_UBLB2_Dir;
        var C2_UBLB1_Dir = context.C2_UBLB1_Dir;
        var C2_UBLB2_Dir = context.C2_UBLB2_Dir;
        var C3_UBLB1_Dir = context.C3_UBLB1_Dir;
        var C4_UBLB1_Dir = context.C4_UBLB1_Dir;
        var C4_UBLB2_Dir = context.C4_UBLB2_Dir;
        var C5_UBLB1_Dir = context.C5_UBLB1_Dir;
        var C5_UBLB2_Dir = context.C5_UBLB2_Dir;
        var C8_UBLB1_Dir = context.C8_UBLB1_Dir;
        var C8_UBLB2_Dir = context.C8_UBLB2_Dir;
        var L1_UBLB1_Dir = context.L1_UBLB1_Dir;
        var pC1_UBLB2_Dir = context.pC1_UBLB2_Dir;
        var pL1_UBLB2_Dir = context.pL1_UBLB2_Dir;
        var C13_UBLB1_Dir = context.C13_UBLB1_Dir;
        var pC1_UBLB1_Dir = context.pC1_UBLB1_Dir;
        var C3_SEM_MA_ConfBreak = context.C3_SEM_MA_ConfBreak;
        var C8_SEM_MA_ConfBreak = context.C8_SEM_MA_ConfBreak;
        var C4_C12_Lower = context.C4_C12_Lower;
        var pC2_C5_LLB2_Cont_MAC = context.pC2_C5_LLB2_Cont_MAC;
        var LB2C2 = context.LB2C2;
        var C3_C5_LLB2_Cont_MAC = context.C3_C5_LLB2_Cont_MAC;
        var LB2C1 = context.LB2C1;
        var C13_UBLB2_Dir = context.C13_UBLB2_Dir;
        var C3_UBLB2_Dir = context.C3_UBLB2_Dir;
        var C5_C13_S_Cont_MAC = context.C5_C13_S_Cont_MAC;
        var cParam = context.cParam;
        if (basicTrigger == 6.66M)
        {
            if (MAL2 < MAC13)
            {
                if (SEMC8 < SEMC13 && MAL2 < MAC1 && (MAL2 < SEMC2 || MAL2 < SEMC3))
                {
                    return CreateTrigger(context, 6.6649M, -6.6649M, basicTrigger);
                }
                else if (MAL2 < LBC5 && SEMC3 < LBC3 && SEMC2 < MAC2 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 6.6699M, 6.6699M, basicTrigger, StartRule_6_6699);
                }
                else
                {
                    return CreateTrigger(context, 6.6619M, 0M, basicTrigger);
                }
            }
            else if (MAC5 > UBC13)
            {
                if (SEMC5 <= LBC5)
                {
                    if (MAL2 < LBC5 && MAL2 < MAC1 && (MAL2 < SEMC2 || MAL2 < SEMC3))
                    {
                        return CreateTrigger(context, 6.6648M, -6.6648M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6618M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < MAC8)
                {
                    return CreateTrigger(context, 6.6698M, 6.6698M, basicTrigger);
                }
                else if (MAL1 > MAC8 && MAL2 > MAC8 && MAL3 > MAC8 && MAL2 < MAC1 && MAL2 < MAC5 && (MAL2 < SEMC2 || MAL2 < SEMC3))
                {
                    return CreateTrigger(context, 6.6647M, -6.6647M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6617M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= UBC21)
            {
                if (MAL2 >= SEMC5)
                {
                    if ((MAL2 > UBC5 || MAL2 > UBC8) || (MAL1 > UBC5 || MAL1 > UBC8) || (MAL3 > UBC5 || MAL3 > UBC8))
                    {
                        return CreateTrigger(context, 6.6697M, 6.6697M, basicTrigger, StartRule_6_6697);
                    }
                    else if (MAL2 > MAC5)
                    {
                        if (MAC2 < MAC3)
                        {
                            return CreateTrigger(context, 6.6646M, -6.6646M, basicTrigger);
                        }
                        else if (MAC2 >= MAC3)
                        {
                            return CreateTrigger(context, 6.6645M, -6.6645M, basicTrigger);
                        }
                    }
                    else if (MAL2 <= MAC5 && MAL2 >= LBC5)
                    {
                        if (SEMC1 <= MAC1 && SEMC2 < UBC2)
                        {
                            return CreateTrigger(context, 6.6696M, 6.6696M, basicTrigger);
                        }
                        else if (SEMC1 > MAC1 && MAL2 < SEMC1)
                        {
                            return CreateTrigger(context, 6.6644M, -6.6644M, basicTrigger);
                        }
                        else if (SEMC2 > UBC2 && MAC2 < MAC3 && MAC1 < MAC5)
                        {
                            return CreateTrigger(context, 6.6643M, -6.6643M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6609M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < LBC5)
                    {
                        return CreateTrigger(context, 6.6642M, -6.6642M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6616M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < SEMC5)
                {
                    if (SEMC8 > UBC8 && MAL2 > LBC5 && SEMC1 <= MAC1 && SEMC2 <= MAC2 && SEMC3 <= MAC3)
                    {
                        return CreateTrigger(context, 6.6695M, 6.6695M, basicTrigger);
                    }
                    else if (MAL2 < LBC5)
                    {
                        return CreateTrigger(context, 6.6641M, -6.6641M, basicTrigger);
                    }
                    else if ((MAL2 < SEMC1 || MAL2 < SEMC2) && (SEMC1 > MAC1 || SEMC2 > MAC2))
                    {
                        return CreateTrigger(context, 6.6639M, -6.6639M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6615M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.6614M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < UBC21)
            {
                if (SEMC8 > UBC8)
                {
                    if (MAC8 >= UBC21)
                    {
                        return CreateTrigger(context, 6.6694M, 6.6694M, basicTrigger, StartRule_6_6694);
                    }
                    else if (C8_UBLB1_Dir == 1)
                    {
                        return CreateTrigger(context, 6.6693M, 6.6693M, basicTrigger);
                    }
                    else if (C8_UBLB1_Dir == -1 && MAL2 < MAC1 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC1) && MAL2 < LBC2)
                    {
                        return CreateTrigger(context, 6.6638M, -6.6638M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6613M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 >= MAC8)
                {
                    if (C8_UBLB1_Dir == 1)
                    {
                        return CreateTrigger(context, 6.6692M, 6.6692M, basicTrigger);
                    }
                    else if (MAL2 < MAL1 && MAL2 < MAC1 && MAL2 < MAC5 && MAL2 < SEMC2 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 6.6637M, -6.6637M, basicTrigger);
                    }
                    else if (MAL2 < MAL1 && MAL2 < MAC1 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 6.6636M, -6.6636M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6612M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < LBC5 && MAL2 < SEMC5)
                {
                    return CreateTrigger(context, 6.6635M, -6.6635M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6611M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.99M)
        { // done

            // should add a limti to where to short for C2.MA < C13.LB1 like SEMC5 < somrthing of c2ma < c13lb2 or c21lb2
            if (MAC3 > LBC13)
            {
                if (MAL2 > SEMC21 || MAC1 > SEMC21)
                {
                    return CreateTrigger(context, 4.9999M, 4.9999M, basicTrigger);
                }
                else if (MAL2 > MAC13)
                {
                    return CreateTrigger(context, 4.9998M, 4.9998M, basicTrigger);
                }
                else if (MAL2 < MAC13 && MAL2 > LBC13)
                {
                    return CreateTrigger(context, 4.9919M, 0, basicTrigger);
                }
                else if (MAL2 <= LBC13)
                {
                    if (SEMC8 < LBC5 && SEMC13 > MAC13)
                    {
                        return CreateTrigger(context, 4.9997M, 4.9997M, basicTrigger);
                    }
                    else if (SEMC8 < LBC5 && MAC2 > LBC13)
                    {
                        return CreateTrigger(context, 4.9996M, 4.9996M, basicTrigger);
                    }
                    else if (SEMC8 > LBC13 && SEMC8 > LBC5 && SEMC13 < MAC13)
                    {
                        return CreateTrigger(context, 4.9995M, 4.9995M, basicTrigger);
                    }
                    else if (SEMC8 > LBC5 && SEMC13 > MAC13)
                    {
                        return CreateTrigger(context, 4.9949M, -4.9949M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9918M, 0, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.9917M, 0, basicTrigger);
                }
            }
            else if (MAC3 <= LBC13)
            {
                if (SEMC13 < LBC13)
                {
                    if (SEMC2 < LB2C5 && MAL2 < SEMC5 && MAL2 > LB2C5 && SEMC3 < SEMC5
                    && SEMC3 < SEMC4 && SEMC4 < SEMC5 && SEMC3 < MAL1)
                    {
                        if (SEMC1 < SEMC2)
                        {
                            return CreateTrigger(context, 4.9994M, 4.9994M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.9916M, 0, basicTrigger);
                        }
                    }
                    else if (SEMC13 < MAC3 && SEMC8 > SEMC13 && SEMC8 > LBC5 && SEMC2 < LB2C5
                    && (MAL3 > LBC1 || MAL3 > MAL2))
                    {
                        return CreateTrigger(context, 4.9993M, 4.9993M, basicTrigger);
                    }
                    else if ((SEMC13 < MAC3 || SEMC13 < LBC8) && SEMC8 < LBC5 && SEMC1 < LB2C2 && SEMC2 < LB2C5)
                    {
                        return CreateTrigger(context, 4.9992M, 4.9992M, basicTrigger);
                    }
                    else if (SEMC13 > MAC3)
                    {
                        if (SEMC8 < SEMC13 && SEMC13 < SEMC21)
                        {
                            return CreateTrigger(context, 4.9948M, -4.9948M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.9915M, 0, basicTrigger);
                        }
                    }
                    else if (SEMC13 <= MAC3)
                    {
                        if (SEMC1 > LB2C2)
                        {
                            return CreateTrigger(context, 4.9947M, -4.9947M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.9914M, 0, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9913M, 0, basicTrigger);
                    }
                }
                else if (SEMC13 >= LBC13)
                {
                    if (SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC3 < SEMC5 && SEMC5 < SEMC8)
                    {
                        return CreateTrigger(context, 4.9991M, 4.9991M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9912M, 0, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.9911M, 0, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.98M)
        {
            if (SEMC5 < LB2C8 && SEMC3 < LB2C5 && SEMC2 < LB2C4 && SEMC1 < LB2C3)
            {
                return CreateTrigger(context, 4.9899M, 4.9899M, basicTrigger);
            }
            else if (SEMC3 > Math.Min(LB2C13, LB2C21) && MAL2 > LB2C5 && MAC1 > SEMC4 && MAL2 > MAC1 && MAL1 > MAC1 && MAL3 > MAL2 && MAL2 > MAL1 && SEMC1 > MAC1 && SEMC1 < UBC1)
            {
                return CreateTrigger(context, 4.9898M, 4.9898M, basicTrigger);
            }
            else if (SEMC3 <= Math.Min(LB2C13, LB2C21) && MAL2 > LB2C5 && MAC1 > SEMC4 && MAC1 > SEMC5 && SEMC2 < LB2C5)
            {
                return CreateTrigger(context, 4.9897M, 4.9897M, basicTrigger);
            }
            else if (MAL2 > LBC2 && SEMC1 > MAC1 && MAL3 < MAC1 && MAL2 < MAC1 && MAL1 < MAC1 && MAL3 < MAL2 && MAL2 < MAL1 && SEMC1 < UBC1)
            {
                return CreateTrigger(context, 4.9896M, 4.9896M, basicTrigger);
            }
            else if (SEMC5 < LB2C8)
            {
                if ((MAC2 < LBC13 || MAC2 < LBC5) && MAL2 < Math.Max(SEMC5, LB2C8))
                {
                    return CreateTrigger(context, 4.9849M, -4.9849M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.9819M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 > LB2C5)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 < LBC13 && SEMC1 > MAC1 && MAL2 < SEMC1 && MAL2 < Math.Max(SEMC5, MAC2))
                    {
                        return CreateTrigger(context, 4.9848M, -4.9848M, basicTrigger);
                    }
                    else if (MAC2 < LBC13 && SEMC1 < MAC1 && MAL2 < Math.Max(SEMC5, MAC2) && MAL3 < MAC1 && MAL2 < MAC1 && MAL1 < MAC1)
                    {
                        return CreateTrigger(context, 4.9847M, -4.9847M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9818M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 > LBC13 && pC2_C5_LLB2_Cont_MAC == 0 && MAL2 < Math.Max(SEMC5, MAC2))
                    {
                        return CreateTrigger(context, 4.9846M, -4.9846M, basicTrigger);
                    }
                    else if (MAC2 < LBC13 && pC2_C5_LLB2_Cont_MAC == 0 && MAL2 < Math.Max(SEMC5, MAC2))
                    {
                        return CreateTrigger(context, 4.9845M, -4.9845M, basicTrigger);
                    }
                    else if (MAC2 < LBC13 && pC2_C5_LLB2_Cont_MAC == 1 && MAL2 < Math.Max(SEMC5, MAC2))
                    {
                        return CreateTrigger(context, 4.9844M, -4.9844M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9817M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC1 > MAC1)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 < LBC13 && MAL2 < SEMC1 && MAL2 < Math.Max(SEMC5, MAC2))
                    {
                        return CreateTrigger(context, 4.9843M, -4.9843M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9816M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAL2 > SEMC4 && MAC1 > SEMC4 && MAL1 > MAC1 && MAL2 > MAC1 && MAL3 < UBC1)
                    {
                        return CreateTrigger(context, 4.9895M, 4.9895M, basicTrigger, StartRule_4_9895);
                    }
                    else if (MAL2 > SEMC4 && MAL3 > MAL1 && MAL3 > MAL2 && SEMC1 < SEMC5 && SEMC2 > LB2C4 && SEMC2 < LB2C5)
                    {
                        return CreateTrigger(context, 4.9894M, 0M, basicTrigger);
                    }
                    else if (MAC1 < SEMC4)
                    {
                        return CreateTrigger(context, 4.9842M, -4.9842M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9815M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAL2 < LB2C5)
            {
                if (SEMC3 <= Math.Min(LB2C13, LB2C21))
                {
                    if (MAC2 > LBC5)
                    {
                        if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && (MAL1 > LB2C5 || MAL2 > LB2C4))
                        {
                            return CreateTrigger(context, 4.9893M, 4.9893M, basicTrigger, StartRule_4_9893);
                        }
                        else if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && MAL1 < LB2C5 && MAL2 < LB2C4 && MAL3 > LB2C2 && SEMC1 < LB2C2)
                        {
                            return CreateTrigger(context, 4.9892M, 4.9892M, basicTrigger);
                        }
                        else if (SEMC5 > SEMC8 || SEMC4 > SEMC5)
                        {
                            if (LB2C5 < LB2C8 && LB2C8 < LB2C13)
                            {
                                return CreateTrigger(context, 4.9891M, 4.9891M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 4.9841M, -4.9841M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 4.9814M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC2 <= LBC5)
                    {
                        if (MAL2 < SEMC4 && LB2C5 > LB2C8)
                        {
                            return CreateTrigger(context, 4.9839M, -4.9839M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.9813M, 0M, basicTrigger);
                        }
                    }
                }
                else if (SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    if (MAL1 < LB2C5 && MAL3 > MAL1)
                    {
                        return CreateTrigger(context, 4.9889M, 4.9889M, basicTrigger);
                    }
                    else if (SEMC5 > LBC13 && MAC2 > LBC8 && MAC5 > MAC8 && C13_UBLB2_Dir == 1)
                    {
                        return CreateTrigger(context, 4.9888M, 4.9888M, basicTrigger);
                    }
                    else if (MAC1 < LB2C8 && MAL2 < SEMC4 && UBC1 > SEMC4)
                    {
                        return CreateTrigger(context, 4.9838M, -4.9838M, basicTrigger);
                    }
                    else if (MAL2 < SEMC4 && UBC1 > SEMC4)
                    {
                        return CreateTrigger(context, 4.9837M, -4.9837M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9812M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC3 <= Math.Min(LB2C13, LB2C21))
            {
                if (MAL2 < SEMC4 && UBC1 > SEMC4)
                {
                    return CreateTrigger(context, 4.9836M, -4.9836M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.9835M, -4.9835M, basicTrigger, StartRule_4_9835);
                }
            }
            else if (SEMC3 > Math.Min(LB2C13, LB2C21))
            {
                if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == 1)
                {
                    if (C5_MAC_Pivot_Trend == 1)
                    {
                        if (SEMC5 < LBC5 && SEMC5 < C5_MAC_SupRes && MAC1 < LBC5 && MAC1 > LB2C8 && MAL2 > LB2C8)
                        {
                            return CreateTrigger(context, 4.9887M, 4.9887M, basicTrigger);
                        }
                        else if (SEMC5 < C5_MAC_SupRes && MAC1 < LBC5 && MAC1 > LB2C8 && MAL2 > LB2C8)
                        {
                            return CreateTrigger(context, 4.9886M, 4.9886M, basicTrigger);
                        }
                        else
                        {
                            if ((MAL2 < SEMC4 && UBC1 > SEMC4) || MAC1 < LB2C8)
                            {
                                return CreateTrigger(context, 4.9834M, -4.9834M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 4.9811M, 0M, basicTrigger);
                            }
                        }
                    }
                    else if (C5_MAC_Pivot_Trend == -1)
                    {
                        return CreateTrigger(context, 4.9809M, 0M, basicTrigger);
                    }
                }
                else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1)
                {
                    if (MAC2 < MAC5 && MAC1 > SEMC4 && UBC1 < SEMC5 && SEMC2 < LB2C4 && MAL2 > LB2C8 && MAL2 > LB2C5)
                    {
                        return CreateTrigger(context, 4.9885M, 4.9885M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9808M, 0M, basicTrigger);
                    }
                }
                else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == -1)
                {
                    if ((MAL2 < SEMC4 && UBC1 > SEMC4) || MAC1 < LB2C8 || (MAL2 < Math.Min(SEMC5, MAC1) && MAL2 < MAL1 && SEMC4 < MAC1))
                    {
                        return CreateTrigger(context, 4.9833M, -4.9833M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9807M, 0M, basicTrigger);
                    }
                }
                else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == 1)
                {
                    if ((MAL2 < SEMC4 && UBC1 > SEMC4) || MAC1 < LB2C8 || (MAL2 < Math.Min(SEMC5, MAC1) && MAL2 < MAL1 && SEMC4 < MAC1))
                    {
                        return CreateTrigger(context, 4.9832M, -4.9832M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9806M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 4.96M)
        {
            if (SEMC8 > SEMC13 && (SEMC8 > LBC8 || SEMC13 > LBC8) && SEMC13 < LBC13 && SEMC13 < SEMC21 && SEMC21 < MAC21 && SEMC5 < LBC5 && SEMC5 < LBC8)
            {
                return CreateTrigger(context, 4.9649M, -4.9649M, basicTrigger);
            }
            else if (SEMC8 < LBC8 && SEMC8 < LBC13 && MAC3 < LBC13 && SEMC5 < LBC5 && SEMC5 < LBC8 && SEMC13 > LBC13 && SEMC21 > MAC21 && SEMC3 > LB2C5 && SEMC13 > MAC3)
            {
                return CreateTrigger(context, 4.9648M, -4.9648M, basicTrigger);
            }
            else if (SEMC13 < MAC3)
            {
                if (SEMC8 < MAC2 && SEMC5 < MAC1 && SEMC5 < LBC3)
                {
                    return CreateTrigger(context, 4.9699M, 4.9699M, basicTrigger);
                }
                else if (SEMC8 < LBC3 && MAL2 > MAC1 && MAL2 > MAL1)
                {
                    return CreateTrigger(context, 4.9698M, 4.9698M, basicTrigger);
                }
                else if (MAC2 < LBC5 && SEMC5 < MAC1 && SEMC5 < LBC3 && SEMC5 > LBC1)
                {
                    return CreateTrigger(context, 4.9697M, 4.9697M, basicTrigger);
                }
                else if (MAC2 > LBC5 && SEMC8 > MAC2 && SEMC8 > LBC5 && MAL2 < MAL1)
                {
                    return CreateTrigger(context, 4.9647M, -4.9647M, basicTrigger);
                }
                else if (MAL2 < SEMC2 && MAL2 < LBC2 && MAL2 < MAL1)
                {
                    return CreateTrigger(context, 4.9646M, -4.9646M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.9619M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 < LBC13)
            {
                if (MAC2 < LB2C13)
                {
                    return CreateTrigger(context, 4.9696M, 0M, basicTrigger);
                }
                else if (SEMC5 < MAC1 && SEMC5 > LBC1 && SEMC8 < MAC1 && SEMC8 > LBC1 && SEMC3 < SEMC5 && SEMC4 < SEMC5 && SEMC1 < UBC1)
                {
                    return CreateTrigger(context, 4.9695M, 4.9695M, basicTrigger);
                }
                else if (SEMC13 > MAC3 && SEMC21 > MAC3)
                {
                    if ((MAC1 > LB2C13 || MAC1 > LB2C21) && MAL2 > MAC1 && SEMC1 > MAC1 && SEMC13 > SEMC21)
                    {
                        return CreateTrigger(context, 4.9618M, 0M, basicTrigger);
                    }
                    else if (MAC1 < LB2C13 && MAC1 < LB2C21)
                    {
                        return CreateTrigger(context, 4.9645M, -4.9645M, basicTrigger);
                    }
                    else if (MAL2 < MAL1 || MAL2 < MAC1 || MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 4.9644M, -4.9644M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9643M, -4.9643M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.9617M, 0M, basicTrigger);
                }
            }
            else if (cParam.C5SEM_LB2C13C21_conf == 0)
            {
                if (MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 4.9694M, 4.9694M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.9693M, 4.9693M, basicTrigger);
                }
            }
            else if (SEMC13 > LBC13 && SEMC5 < MAC1 && SEMC5 > LBC1)
            {
                return CreateTrigger(context, 4.9692M, 4.9692M, basicTrigger);
            }
            else if (SEMC13 > LBC13 && SEMC5 < MAC1 && SEMC5 < LBC1 && SEMC1 > SEMC5)
            {
                return CreateTrigger(context, 4.9691M, 4.9691M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9616M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.94M)
        {
            if (cParam.C2_LLB2_stop == -1)
            {
                return CreateTrigger(context, 4.9449M, -4.9449M, basicTrigger, StartRule_4_9449);
            }
            else if (SEMC3 < LB2C5)
            {
                return CreateTrigger(context, 4.9499M, 4.9499M, basicTrigger);
            }
            else if (C3_C5_LLB2_Cont_MAC == 1)
            {
                if (MAC2 < LBC5)
                {
                    if (SEMC1 < MAC1 || SEMC3 > SEMC5)
                    {
                        return CreateTrigger(context, 4.9498M, 4.9498M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.9446M, -4.9446M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.9497M, 4.9497M, basicTrigger);
                }
            }
            else if (SEMC1 > SEMC2 && SEMC1 < MAC1)
            {
                return CreateTrigger(context, 4.9496M, 4.9496M, basicTrigger);
            }
            else if (MAL2 < SEMC5 && SEMC1 > MAC1 && MAL2 < SEMC1)
            {
                return CreateTrigger(context, 4.9448M, -4.9448M, basicTrigger);
            }
            else if (MAL2 < LBC5 && MAC2 < LBC5 && SEMC1 > MAC1 && MAL2 < SEMC1)
            {
                return CreateTrigger(context, 4.9447M, -4.9447M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9419M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.93M)
        {
            if (SEMC4 > SEMC5)
            {
                return CreateTrigger(context, 4.9349M, -4.9349M, basicTrigger);
            }
            else if (MAL3 < SEMC4 && MAL2 < SEMC4 && MAL1 < SEMC4)
            {
                return CreateTrigger(context, 4.9348M, -4.9348M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9399M, 4.9399M, basicTrigger);
            }
        }

        if (basicTrigger == 4.92M)
        {
            if (MAL2 > SEMC1 && MAL2 > SEMC2 && MAL2 > SEMC3)
            {
                return CreateTrigger(context, 4.9299M, 4.9299M, basicTrigger);
            }
            else if (MAL2 < SEMC1 && SEMC1 > MAC1 && MAL3 < MAL2 && MAL3 < MAL1)
            {
                return CreateTrigger(context, 4.9249M, -4.9249M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.91M)
        {
            if (MAC2 < LBC5)
            {
                if (SEMC1 < MAC1 || SEMC3 > SEMC5)
                {
                    return CreateTrigger(context, 4.9199M, 4.9199M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.9149M, -4.9149M, basicTrigger);
                }
            }
            else if (SEMC1 < MAC1 || SEMC3 > SEMC5 || SEMC1 > SEMC2 || SEMC1 > SEMC3)
            {
                return CreateTrigger(context, 4.9198M, 4.9198M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9119M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.88M)
        {
            if (SEMC3 > MAC1)
            {
                return CreateTrigger(context, 4.8849M, -4.8849M, basicTrigger);
            }
            else if (MAL2 > MAC1 && MAL1 > MAC1)
            {
                if (UBC1 > SEMC4 && MAL2 > MAL1 && MAL2 < SEMC1 && SEMC3 > LB2C5 && MAL3 < UBC1 && (SEML1 < UBC1 || SEMC1 < UBC1))
                {
                    return CreateTrigger(context, 4.8899M, 4.8899M, basicTrigger, StartRule_4_8899);
                }
                else if (UBC1 < SEMC4 && MAL3 < UBC1 && SEMC1 < UBC1)
                {
                    return CreateTrigger(context, 4.8898M, 4.8898M, basicTrigger, StartRule_4_8898, additionalStopCondition: StopRule_4_8898);
                }
                else
                {
                    return CreateTrigger(context, 4.8848M, -4.8848M, basicTrigger);
                }
            }
            else if (MAL1 > MAC1)
            {
                return CreateTrigger(context, 4.8819M, 0M, basicTrigger);
            }
            else if (UBC1 < SEMC4 && MAL3 < UBC1 && SEMC1 < UBC1)
            {
                return CreateTrigger(context, 4.8897M, 4.8897M, basicTrigger, StartRule_4_8897);
            }
            else if (MAL2 < SEMC4)
            {
                return CreateTrigger(context, 4.8847M, -4.8847M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.8818M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.87M)
        {
            if (MAL2 < SEMC2)
            {
                return CreateTrigger(context, 4.8749M, -4.8749M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.8719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.86M)
        {
            if (LB2C3 < LB2C5 && LB2C5 < LB2C8 && LB2C8 < LB2C13 && LB2C13 < LB2C21)
            {
                return CreateTrigger(context, 4.8699M, 4.8699M, basicTrigger, StartRule_4_8699);
            }
            else if (SEMC3 <= Math.Min(LB2C13, LB2C21))
            {
                return CreateTrigger(context, 4.8619M, 0M, basicTrigger);
            }
            else if (LB2C3 > LB2C5)
            {
                if (MAC2 > MAC5 && MAC3 > MAC5 && MAC1 > LBC5 && MAL1 > LBC5 && LBC2 > LBC5)
                {
                    return CreateTrigger(context, 4.8698M, 4.8698M, basicTrigger, StartRule_4_8698);
                }
                else if (MAC3 < LBC13 && MAC3 < LBC21)
                {
                    return CreateTrigger(context, 4.8697M, 4.8697M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8649M, -4.8649M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.8648M, -4.8648M, basicTrigger);
            }
        }

        if (basicTrigger == 4.85M)
        {
            if (C3_C5_LLB2_Cont_MAC == 1)
            {
                return CreateTrigger(context, 4.8599M, 4.8599M, basicTrigger);
            }
            else if (LB2C2 < LB2C3 && LB2C2 < LB2C4 && LB2C2 < LB2C5 && LB2C2 < LB2C8 && LB2C2 < LB2C13 && SEMC2 < SEMC3 && SEMC2 < SEMC4)
            {
                return CreateTrigger(context, 4.8598M, 4.8598M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == 1)
            {
                if (MAC1 > MAC3 && MAC3 > MAC5 && C3_UBLB1_Dir == 1 && C4_UBLB1_Dir == 1 && SEMC3 < UBC3 && SEMC3 > MAC3 && MAL2 > MAC5)
                {
                    return CreateTrigger(context, 4.8597M, 4.8597M, basicTrigger);
                }
                else if (SEMC5 < MAC5 && SEMC4 < MAC4)
                {
                    if (SEMC3 < SEMC4 || SEMC3 < SEMC5)
                    {
                        return CreateTrigger(context, 4.8549M, -4.8549M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.8519M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 < MAC3)
                {
                    if (MAC2 > MAC3)
                    {
                        return CreateTrigger(context, 4.8596M, 4.8596M, basicTrigger);
                    }
                    else if (MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 4.8548M, -4.8548M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.8518M, 0M, basicTrigger);
                    }
                }
                else if (MAC1 > MAC3 && MAC3 > MAC5 && (C3_UBLB1_Dir == 1 || C4_UBLB1_Dir == 1) && SEMC3 < UBC3 && SEMC3 > MAC3 && SEMC2 < MAC3 && SEMC1 < LB2C2 && MAL2 > MAC4)
                {
                    return CreateTrigger(context, 4.8595M, 4.8595M, basicTrigger);
                }
                else if (LB2C2 > LB2C3 && MAL2 < LBC2 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.8547M, -4.8547M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8517M, 0M, basicTrigger);
                }
            }
            else if (LB2C2 > LB2C3 && MAL2 < LBC2 && MAL2 < SEMC2)
            {
                return CreateTrigger(context, 4.8546M, -4.8546M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.8516M, 0M, basicTrigger);
            }
            else if ((C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == -1) || (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == 1))
            {
                if (MAL2 < LBC2 && MAL2 < SEMC2 && SEMC2 < SEMC3)
                {
                    return CreateTrigger(context, 4.8545M, -4.8545M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8515M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.84M)
        {
            if (SEMC5 <= LBC5 && UBC1 <= LBC5)
            {
                if (MAL1 < SEMC4 && MAL2 < SEMC4 && MAL1 > SEMC3 && MAL2 > SEMC3 && SEMC4 > LBC2)
                {
                    return CreateTrigger(context, 4.8499M, 4.8499M, basicTrigger);
                }
                else if ((C5_C21_Flag_Trend_Lower == 1 || C3_C13_Flag_Trend_Lower == 1) && ((MAL2 < SEMC4 && UBC1 > SEMC4) || MAC1 < LBC8 || (MAL2 < Math.Min(SEMC5, MAC1) && MAL2 < MAL1 && SEMC4 < MAC1)) && SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    return CreateTrigger(context, 4.8449M, -4.8449M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8419M, 0M, basicTrigger);
                }
            }
            else
            {
                if (MAL1 > SEMC3 && MAL2 > SEMC3)
                {
                    return CreateTrigger(context, 4.8498M, 4.8498M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8418M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.83M)
        {
            if (SEMC2 < LB2C4)
            {
                if (SEMC2 < MAC1 && SEMC3 < MAC1 && SEMC4 < MAC1 && SEMC5 < MAC1)
                {
                    return CreateTrigger(context, 4.8399M, 4.8399M, basicTrigger, additionalStopCondition: StopRule_4_8399);
                }
                else if (MAL3 > MAL2 && MAL2 > MAL1)
                {
                    return CreateTrigger(context, 4.8398M, 4.8398M, basicTrigger, additionalStopCondition: StopRule_4_8398);
                }
                else if (SEMC1 < LB2C4)
                {
                    return CreateTrigger(context, 4.8397M, 4.8397M, basicTrigger, additionalStopCondition: StopRule_4_8397);
                }
                else if ((C5_C21_Flag_Trend_Lower == 1 || C3_C13_Flag_Trend_Lower == 1) && ((MAL2 < SEMC4 && UBC1 > SEMC4) || MAC1 < LBC8 || (MAL2 < Math.Min(SEMC5, MAC1) && MAL2 < MAL1 && SEMC4 < MAC1)) && SEMC3 > Math.Min(LB2C13, LB2C21))
                {
                    return CreateTrigger(context, 4.8349M, -4.8349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8319M, 0M, basicTrigger);
                }
            }
            else if (SEMC4 > LBC8 || MAL2 > LBC8 || MAC1 > LBC8)
            {
                return CreateTrigger(context, 4.8396M, 4.8396M, basicTrigger, additionalStopCondition: StopRule_4_8396);
            }
            else
            {
                return CreateTrigger(context, 4.8348M, -4.8348M, basicTrigger);
            }
        }

        if (basicTrigger == 4.82M)
        {
            if (LB2C4 <= LB2C5)
            {
                if (MAC1 > LBC8 && SEMC3 > LBC8)
                {
                    return CreateTrigger(context, 4.8299M, 4.8299M, basicTrigger, additionalStopCondition: StopRule_4_8299);
                }
                else if (MAC1 > LBC4 || SEML3 > LBC3)
                {
                    return CreateTrigger(context, 4.8249M, -4.8249M, basicTrigger);
                }
                else if (SEMC2 < LB2C3)
                {
                    return CreateTrigger(context, 4.8248M, -4.8248M, basicTrigger);
                }
                else if (MAL3 < LBC1)
                {
                    return CreateTrigger(context, 4.8247M, -4.8247M, basicTrigger);
                }
                else if (SEMC4 > LBC4)
                {
                    return CreateTrigger(context, 4.8246M, -4.8246M, basicTrigger);
                }
                else if (MAL3 < LB2C3)
                {
                    return CreateTrigger(context, 4.8296M, 4.8296M, basicTrigger, StartRule_4_8296, additionalStopCondition: StopRule_4_8296);
                }
                else
                {
                    return CreateTrigger(context, 4.8298M, 4.8298M, basicTrigger, additionalStopCondition: StopRule_4_8298);
                }
            }
            else if (LB2C4 > LB2C5)
            {
                if (MAL2 > LBC8)
                {
                    return CreateTrigger(context, 4.8297M, 4.8297M, basicTrigger, additionalStopCondition: StopRule_4_8297);
                }
                else if (MAC1 > LBC4 || SEML3 > LBC3 || SEMC2 < LB2C3 || MAL3 < LBC1 || SEMC4 > LBC4 || (MAL1 < LB2C4 && MAL2 < LB2C4 && MAL3 < LB2C4))
                {
                    return CreateTrigger(context, 4.8245M, -4.8245M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.8219M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.78M)
        {
            if (MAC2 > LBC13 && MAC1 > LB2C13 && MAL1 > LB2C21 && SEMC3 < LBC2)
            {
                return CreateTrigger(context, 4.7899M, 4.7899M, basicTrigger);
            }
            else if ((MAC2 < LBC13 || MAC1 < LB2C13 || MAL1 < LB2C21) && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 > LB2C3)
            {
                if (SEMC3 < LBC2)
                {
                    return CreateTrigger(context, 4.7849M, -4.7849M, basicTrigger, StartRule_4_7849);
                }
                else
                {
                    return CreateTrigger(context, 4.7848M, -4.7848M, basicTrigger);
                }
            }
            else if (SEMC3 < LBC2)
            {
                return CreateTrigger(context, 4.7847M, -4.7847M, basicTrigger);
            }
            else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 > LB2C3)
            {
                return CreateTrigger(context, 4.7846M, -4.7846M, basicTrigger);
            }
            else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 < LB2C3)
            {
                return CreateTrigger(context, 4.7845M, -4.7845M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.7819M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.77M)
        {
            if (MAC2 > LBC13 && MAC1 > LB2C13 && MAL1 > LB2C21 && MAL2 < LB2C4 && SEMC3 < LBC3)
            {
                return CreateTrigger(context, 4.7799M, 4.7799M, basicTrigger, additionalStopCondition: StopRule_4_7799);
            }
            else if (MAC2 < LBC13 || MAC1 < LB2C13 || MAL1 < LB2C21)
            {
                if (SEMC3 < LBC2)
                {
                    return CreateTrigger(context, 4.7749M, -4.7749M, basicTrigger, StartRule_4_7749);
                }
                else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 > LB2C3)
                {
                    return CreateTrigger(context, 4.7748M, -4.7748M, basicTrigger);
                }
                else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 < LB2C3)
                {
                    return CreateTrigger(context, 4.7747M, -4.7747M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7719M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 > LB2C3)
            {
                return CreateTrigger(context, 4.7746M, -4.7746M, basicTrigger);
            }
            else if (SEMC3 > LBC2 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 < LB2C3)
            {
                return CreateTrigger(context, 4.7745M, -4.7745M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.7718M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.76M)
        {
            return CreateTrigger(context, 4.7699M, 4.7699M, basicTrigger);
        }

        if (basicTrigger == 4.75M)
        {
            return CreateTrigger(context, 4.7599M, 4.7599M, basicTrigger);
        }

        if (basicTrigger == 4.74M)
        {
            if (MAC1 > MAC3)
            {
                if (SEMC3 > MAC3 && MAL2 > SEMC3 && MAL3 > SEMC3 && SEMC1 > SEMC3 && SEMC2 > SEMC3)
                {
                    return CreateTrigger(context, 4.7499M, 4.7499M, basicTrigger);
                }
                else if (SEMC3 < MAC3 && MAL2 < SEMC2 && SEMC2 > MAC2)
                {
                    return CreateTrigger(context, 4.7449M, -4.7449M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7419M, 0M, basicTrigger);
                }
            }
            else if (MAC1 > MAC2)
            {
                if (SEMC5 < MAC5 && MAL2 < MAC1 && SEMC1 < MAC1 && MAL2 < SEMC2 && C2_SEMShortTrend != 1 && MAL3 < SEMC1 && MAL3 < LBC1 && SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 4.7448M, -4.7448M, basicTrigger);
                }
                else if (cParam.C5SEM_LB2C13C21_conf == 0)
                {
                    if (MAL3 > SEMC2 && (MAL2 > MAC1 || SEMC1 > MAC1))
                    {
                        return CreateTrigger(context, 4.7498M, 4.7498M, basicTrigger);
                    }
                    else if (MAL3 < SEMC2 && MAL2 < SEMC1)
                    {
                        return CreateTrigger(context, 4.7447M, -4.7447M, basicTrigger, StartRule_4_7447);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.7418M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 > MAC2 && (MAL2 > SEMC2 || MAL2 < MAC1 || (SEMC1 < MAC1 && MAL2 > SEMC1)))
                {
                    return CreateTrigger(context, 4.7497M, 4.7497M, basicTrigger);
                }
                else if (C3_C5_LLB2_Cont_MAC == 1)
                {
                    if (SEMC2 > MAC2)
                    {
                        return CreateTrigger(context, 4.7495M, 4.7495M, basicTrigger);
                    }
                    else if (SEMC2 < MAC2 && SEMC1 > MAC1 && MAL2 < SEMC1 && MAL3 < SEMC1 && MAL3 < MAL2)
                    {
                        return WithSideCandidates(
                            CreateTrigger(context, 4.7446M, 4.7446M, basicTrigger, StartRule_4_7446_Positive, additionalStopCondition: StopRule_4_7446),
                            CreateTrigger(context, 4.7446M, -4.7446M, basicTrigger, StartRule_4_7446_Negative));
                    }
                    else
                    {
                        return CreateTrigger(context, 4.7417M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 > MAC2 && MAL2 < SEMC2 && MAL3 < SEMC2 && MAL2 > MAC1 && SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 4.7445M, -4.7445M, basicTrigger);
                }
                else if (SEMC2 < MAC2 && MAL3 < MAL2)
                {
                    return CreateTrigger(context, 4.7444M, -4.7444M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7416M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.73M)
        {
            if (MAC2 > MAC3)
            {
                if (SEMC1 < MAC1 && SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 4.7399M, 4.7399M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7349M, -4.7349M, basicTrigger);
                }
            }
            else if (MAC1 >= MAC2 && SEMC2 > MAC2)
            {
                if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.7398M, 4.7398M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7348M, -4.7348M, basicTrigger);
                }
            }
            else if (MAC1 < MAC2 && MAC2 < MAC3)
            {
                if (SEMC2 < MAC2 && MAL3 < MAL2 && (MAL2 > SEMC2 || MAL3 > SEMC2))
                {
                    return CreateTrigger(context, 4.7397M, 4.7397M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7347M, -4.7347M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.7319M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.72M)
        {
            if (MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > SEMC4 && MAL2 > SEMC5 && MAL2 > MAC1 && MAL2 > MAC2 && MAL2 > MAC3 && MAL2 > MAC5 && SEMC13 > SEMC21 && SEMC8 > SEMC21 && SEMC5 > SEMC21 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC2 > SEMC3)
            {
                return CreateTrigger(context, 4.7299M, 4.7299M, basicTrigger);
            }
            else if (SEMC5 > SEMC8 && SEMC5 < MAC5 && MAL2 > SEMC5 && MAC1 < MAC5)
            {
                return CreateTrigger(context, 4.7298M, 4.7298M, basicTrigger);
            }
            else if (SEMC5 > SEMC21 && MAC5 > SEMC21 && SEMC21 > MAC21 && SEMC1 < MAC1 && SEMC5 < UBC1 && MAC1 > LBC5 && SEMC8 > SEMC21 && SEMC13 > SEMC21 && (C5_UBLB1_Dir == 1 || (MAC1 > MAC2 && MAC1 > MAC3 && MAC1 > MAC5)))
            {
                return CreateTrigger(context, 4.7297M, 4.7297M, basicTrigger);
            }
            else if (SEMC5 > SEMC8 && SEMC5 > MAC5 && SEMC2 > SEMC3 && SEMC3 > SEMC4 && MAL2 > SEMC5 && MAL2 < MAL1 && MAL2 < LBC89)
            {
                return CreateTrigger(context, 4.7296M, 4.7296M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == 1)
            {
                if (UBC1 < MAC2 && SEMC5 < MAC1)
                {
                    return CreateTrigger(context, 4.7249M, -4.7249M, basicTrigger);
                }
                else if (MAL2 < SEMC1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.7248M, -4.7248M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7247M, -4.7247M, basicTrigger, StartRule_4_7247);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1)
            {
                if (UBC1 < MAC2 && SEMC5 < MAC1)
                {
                    return CreateTrigger(context, 4.7219M, 0M, basicTrigger);
                }
                else if (MAL2 < SEMC1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.7246M, -4.7246M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7245M, -4.7245M, basicTrigger);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 4.7244M, -4.7244M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == -1)
            {
                if (SEMC5 < MAC1 && (SEMC13 > SEMC89 || SEMC21 > SEMC89 || SEMC8 > SEMC89))
                {
                    return CreateTrigger(context, 4.7295M, 4.7295M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7243M, -4.7243M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.71M)
        {
            if (SEMC2 < LB2C4 && SEMC1 > MAC1 && UBC1 < SEMC5)
            {
                return CreateTrigger(context, 4.7199M, 4.7199M, basicTrigger, StartRule_4_7199);
            }
            else if (SEMC3 < LB2C5 && MAL2 > SEMC3 && MAL2 > SEMC2 && MAL2 > SEMC1)
            {
                return CreateTrigger(context, 4.7198M, 4.7198M, basicTrigger);
            }
            else if (LB2C13 > LB2C21)
            {
                if (MAL2 < MAC1 || MAL2 < MAL1)
                {
                    return CreateTrigger(context, 4.7149M, -4.7149M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.7119M, 0M, basicTrigger);
                }
            }
            else if (LB2C13 <= LB2C21)
            {
                if (MAC5 < LBC13 && MAC5 < LBC21)
                {
                    return CreateTrigger(context, 4.7197M, 4.7197M, basicTrigger, StartRule_4_7197);
                }
                else if (MAC5 > LBC13 && MAC5 > LBC21)
                {
                    if (MAL2 > MAC1 && SEMC1 > MAC1 && SEMC4 < MAC1 && SEMC5 < MAC1 && MAL2 > MAC89 && MAC1 > MAC89 && MAC5 > MAC89)
                    {
                        return CreateTrigger(context, 4.7196M, 4.7196M, basicTrigger);
                    }
                    else if (MAL2 < MAC89 && MAL2 < MAC1 && MAL2 < LB2C13 && MAL2 < LB2C21 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5)
                    {
                        return CreateTrigger(context, 4.7148M, -4.7148M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.7118M, 0M, basicTrigger);
                    }
                }
                else
                {
                    if (SEMC5 < MAC89 && MAL2 < MAC89 && SEMC4 < MAC1 && SEMC5 < MAC1 && MAL2 > MAC1 && MAL2 > MAL1 && SEMC1 > MAC1 && SEMC3 < SEMC5 && SEMC2 > SEMC3 && SEMC3 > LB2C5)
                    {
                        return CreateTrigger(context, 4.7195M, 4.7195M, basicTrigger);
                    }
                    else if (MAL2 < MAC89 && (MAL2 < MAC1 || MAL2 < MAL1) && (MAL2 < LB2C13 || MAL2 < LB2C21))
                    {
                        return CreateTrigger(context, 4.7147M, -4.7147M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.7117M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 4.69M)
        {
            if (SEMC13 < LBC8)
            {
                if (MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.6949M, -4.6949M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6948M, -4.6948M, basicTrigger, StartRule_4_6948);
                }
            }
            else if (MAL2 > SEMC5 && SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 4.6999M, 4.6999M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.6919M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.68M)
        {
            if (SEMC5 >= LBC1)
            {
                if (MAL2 > SEMC5 && SEML2 > SEMC5 && SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 4.6899M, 4.6899M, basicTrigger);
                }
                else if (MAL2 > SEMC5 && SEML2 > SEMC5 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.6895M, 4.6895M, basicTrigger, additionalStopCondition: StopRule_4_6895);
                }
                else if (MAL2 < SEMC5 && SEML2 < SEMC5 && MAL2 < MAC1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.6847M, -4.6847M, basicTrigger);
                }
                else if ((MAL2 < SEMC5 || SEML2 < SEMC5) && MAL2 < MAC1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.6846M, -4.6846M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6819M, 0M, basicTrigger);
                }
            }

            if (SEMC5 < LBC1)
            {
                if (SEMC5 < LB2C8 && SEMC1 < MAC1 && SEMC1 > SEMC5 && MAL2 > SEMC5 && SEML2 > SEMC5 && MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 4.6898M, 4.6898M, basicTrigger);
                }
                else if (SEMC5 < LB2C8 && SEMC1 < MAC1 && SEMC1 > SEMC5 && MAL2 > SEMC5 && SEML2 > SEMC5 && MAL2 < SEMC2)
                {
                    return WithSideCandidates(
                        CreateTrigger(context, 4.6897M, 4.6897M, basicTrigger, additionalStopCondition: StopRule_4_6897),
                        CreateTrigger(context, 4.6897M, -4.6897M, basicTrigger, StartRule_4_6897_Negative, requireZeroSideCounter: false));
                }
                else if (MAL2 > MAC1 && MAL2 > MAL1 && SEMC1 > MAC1 && SEMC1 < UBC1 && SEML2 < MAL2 && SEML1 < MAL2)
                {
                    return CreateTrigger(context, 4.6896M, 4.6896M, basicTrigger);
                }
                else if (MAL1 < MAC1 && MAL2 < MAC1 && MAL3 < MAC1 && MAL2 < MAL1 && SEMC5 > LB2C8)
                {
                    return CreateTrigger(context, 4.6848M, -4.6848M, basicTrigger);
                }
                else if (MAL2 < SEMC1 || MAL2 < SEMC2 || (MAL2 < MAL3 && MAL3 > MAC1 && SEMC1 > MAC1) || (MAL2 < MAL1 && MAL2 < MAC1) || MAL2 < LBC2)
                {
                    return CreateTrigger(context, 4.6849M, -4.6849M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6818M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.67M)
        {
            if (SEMC5 < LB2C8 && SEMC1 < MAC1 && SEMC1 > SEMC5)
            {
                return CreateTrigger(context, 4.6799M, 4.6799M, basicTrigger);
            }
            else if (SEMC5 >= LBC1 && SEMC5 < MAC1 && SEMC1 < UBC1 && MAC2 < LBC5 && SEMC8 < MAC2 && SEMC2 > SEMC5 && SEMC2 > SEMC3 && SEMC2 < MAC2)
            {
                return CreateTrigger(context, 4.6798M, 4.6798M, basicTrigger);
            }
            else if (MAL2 < SEMC1 || MAL2 < SEMC2 || (MAL2 < MAL3 && MAL3 > MAC1))
            {
                return CreateTrigger(context, 4.6749M, -4.6749M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.6719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.65M)
        {
            if ((SEMC13 < LB2C89 || MAC5 < LB2C89))
            {
                if (SEMC5 > Math.Max(SEMC21, SEMC13) || SEMC5 > SEMC21 || SEMC13 > SEMC21)
                {
                    return CreateTrigger(context, 4.6599M, 4.6599M, basicTrigger);
                }
                else if (SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC5 > SEMC8 && (SEMC3 > MAC3 || SEMC2 > MAC2))
                {
                    return CreateTrigger(context, 4.6598M, 4.6598M, basicTrigger, StartRule_4_6598);
                }
                else if (SEMC21 < LBC21 && SEMC5 < SEMC21 && SEMC13 < SEMC21 && SEMC8 < SEMC21 && MAL2 < Math.Max(SEMC21, SEMC13) && MAC2 < MAC3)
                {
                    return CreateTrigger(context, 4.6549M, -4.6549M, basicTrigger);
                }
                else if (SEMC21 > LBC21 && SEMC5 < SEMC21 && SEMC13 < SEMC21 && SEMC8 < SEMC21 && MAL2 < SEMC13 && MAC2 < MAC3)
                {
                    return CreateTrigger(context, 4.6548M, -4.6548M, basicTrigger);
                }
                else if (SEMC21 > LBC21 && SEMC5 < SEMC13 && SEMC5 < SEMC8 && SEMC13 < SEMC21 && SEMC8 < SEMC13 && MAL2 < SEMC2 && MAC2 < MAC3)
                {
                    return CreateTrigger(context, 4.6547M, -4.6547M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6519M, 0M, basicTrigger);
                }
            }
            else if (SEMC89 < LBC89)
            {
                return CreateTrigger(context, 4.6596M, 0M, basicTrigger);
            }
            else if (MAC5 < LBC89)
            {
                if (MAC2 > MAC3)
                {
                    return CreateTrigger(context, 4.6595M, 4.6595M, basicTrigger, StartRule_4_6595);
                }
                else if (SEMC21 < MAC5)
                {
                    return CreateTrigger(context, 4.6594M, 4.6594M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6518M, 0M, basicTrigger);
                }
            }
            else if (MAC5 < MAC89)
            {
                if (MAC2 > MAC3)
                {
                    return CreateTrigger(context, 4.6593M, 4.6593M, basicTrigger, StartRule_4_6593);
                }
                else if (SEMC5 > SEMC13 && SEMC4 > SEMC13 && SEMC4 > SEMC5 && SEMC1 < MAC1 && SEMC2 > SEMC5)
                {
                    return CreateTrigger(context, 4.6592M, 4.6592M, basicTrigger, StartRule_4_6592);
                }
                else if (SEMC5 > SEMC8 && MAL2 > MAL1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.6591M, 4.6591M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.6546M, -4.6546M, basicTrigger);
                }
            }
            else if (SEMC21 < MAC5 && MAC1 < LBC5)
            {
                return CreateTrigger(context, 4.6589M, 4.6589M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.6545M, -4.6545M, basicTrigger);
            }
        }

        if (basicTrigger == 4.62M)
        {
            if (MAL2 <= MAC5)
            {
                if (MAL2 >= MAC1)
                {
                    return CreateTrigger(context, 4.6299M, 4.6299M, basicTrigger);
                }
                else if (MAL2 < MAC1)
                {
                    return CreateTrigger(context, 4.6298M, 4.6298M, basicTrigger);
                }
            }
            else if (MAL2 > MAC5)
            {
                return CreateTrigger(context, 4.6219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.59M)
        {
            if (SEMC89 < LBC89)
            {
                return CreateTrigger(context, 4.5999M, 0M, basicTrigger);
            }
            else if (MAC5 > LBC21 && SEMC5 > MAC5 && C5_SEMTrend != -0.1M && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC21 < LBC21)
            {
                return CreateTrigger(context, 4.5998M, 4.5998M, basicTrigger, StartRule_4_5998);
            }
            else if (SEMC5 > MAC5 && C5_SEMTrend != -0.1M && MAC5 < LBC21 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC1 > MAC1 && SEMC1 > SEMC5 && SEMC1 > SEMC2 && SEMC1 > SEMC3 && SEMC1 > SEMC4 && SEMC21 < LBC21)
            {
                return CreateTrigger(context, 4.5997M, 4.5997M, basicTrigger);
            }
            else if (MAC2 < MAC3 && MAC3 < MAC5 && MAC1 < MAC5 && SEMC5 < LBC5 && SEMC8 < LBC8 && SEMC13 < LBC13 && UBC1 < MAC2)
            {
                return CreateTrigger(context, 4.5996M, 4.5996M, basicTrigger);
            }
            else if (SEMC8 < LBC8 && SEMC5 < LBC5 && SEMC3 < LBC3 && SEMC2 < LBC2 && MAL2 > MAC89)
            {
                return CreateTrigger(context, 4.5987M, 4.5987M, basicTrigger);
            }
            else if (MAC2 > MAC3)
            {
                if (SEMC1 > MAC1 && MAL2 > MAC3 && ((MAL2 > MAL1 && MAL2 > MAC1) || (MAL2 > MAC1 && MAL2 < UBC2)) && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC4 > MAC4 && (SEMC2 > SEMC3 || SEMC3 > SEMC4) && SEMC2 > SEMC5 && (MAL2 > MAC2 || MAL2 > SEMC5) && (MAL2 > MAL1 || MAL2 > SEMC3 || MAL2 < UBC2))
                {
                    return CreateTrigger(context, 4.5995M, 4.5995M, basicTrigger, StartRule_4_5995);
                }
                else if (SEMC1 < MAC1 && MAL2 > MAC3 && SEMC2 > MAC2 && SEMC3 > MAC3 && (SEMC2 > SEMC3 || SEMC3 > SEMC4) && SEMC2 > SEMC5 && (MAL2 > MAC2 || MAL2 > SEMC5))
                {
                    return CreateTrigger(context, 4.5994M, 4.5994M, basicTrigger);
                }
                else if (SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC5 < MAC5 && SEMC2 < SEMC3 && SEMC2 < SEMC5 && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC5 && SEMC2 < LBC2 && SEMC5 < LBC5 && SEMC8 < LBC5 && MAL2 < MAL1 && MAL3 < SEMC2 && UBC1 > SEMC5 && UBC1 > SEMC4 && UBC1 > SEMC3)
                {
                    return CreateTrigger(context, 4.5949M, -4.5949M, basicTrigger);
                }
                else if (SEMC5 > SEMC21 || SEMC8 > SEMC21 || SEMC13 > SEMC21)
                {
                    if (MAL2 <= MAC3)
                    {
                        if (MAL1 > SEMC2 || MAL2 > SEMC2 || MAL3 > SEMC2)
                        {
                            return CreateTrigger(context, 4.5993M, 4.5993M, basicTrigger, StartRule_4_5993);
                        }
                        else if (MAL2 < SEMC2 && MAL3 < SEMC2 && MAL2 < MAL1 && MAL3 < MAL2 && SEMC2 < LBC2)
                        {
                            return CreateTrigger(context, 4.5948M, -4.5948M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.5919M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 > MAC3)
                    {
                        if ((MAL2 < SEMC2 && MAL2 < SEMC1 && MAL2 < MAL1) || (MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC5 && MAL2 < MAC1))
                        {
                            return CreateTrigger(context, 4.5947M, -4.5947M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.5918M, 0M, basicTrigger);
                        }
                    }
                }
                else if (SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC1 > SEMC2 && SEMC2 > SEMC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5 && MAL2 > MAC1 && MAL2 > SEMC3)
                {
                    return CreateTrigger(context, 4.5992M, 4.5992M, basicTrigger);
                }
                else if (SEMC2 < MAC2 && SEMC5 < MAC5 && (SEMC2 > SEMC5 || SEMC3 > SEMC5) && MAL2 > SEMC2 && MAL2 > SEMC5)
                {
                    return CreateTrigger(context, 4.5991M, 4.5991M, basicTrigger);
                }
                else if (SEMC5 > MAC5 && MAC3 > MAC5 && MAC2 > MAC5 && MAL2 > SEMC1 && MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 4.5989M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.5946M, -4.5946M, basicTrigger);
                }
            }
            else if (MAL2 > MAC1 && MAL2 > SEMC1)
            {
                return CreateTrigger(context, 4.5988M, 0M, basicTrigger);
            }
            else if (MAC1 < MAC3 && (MAL2 < MAC1 || MAL2 < SEMC1))
            {
                return CreateTrigger(context, 4.5945M, -4.5945M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.5917M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.57M)
        {
            if (MAL3 < LBC1 && SEMC1 < LBC1)
            {
                return CreateTrigger(context, 4.5719M, 0M, basicTrigger);
            }
            else if (SEMC1 > MAC1)
            {
                return CreateTrigger(context, 4.5749M, -4.5749M, basicTrigger, StartRule_4_5749);
            }
            else
            {
                return CreateTrigger(context, 4.5799M, 4.5799M, basicTrigger);
            }
        }

        if (basicTrigger == 4.56M)
        {
            if (MAC2 > MAC3 && MAC3 > MAC5)
            {
                if (SEMC3 > MAC5)
                {
                    if (MAL2 < SEMC1)
                    {
                        return CreateTrigger(context, 4.5649M, -4.5649M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.5648M, -4.5648M, basicTrigger, StartRule_4_5648);
                    }
                }
                else if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 4.5699M, 4.5699M, basicTrigger, StartRule_4_5699);
                }
                else
                {
                    return CreateTrigger(context, 4.5698M, 4.5698M, basicTrigger, StartRule_4_5698);
                }
            }
            else if (MAC2 < MAC3)
            {
                return CreateTrigger(context, 4.5647M, -4.5647M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.5619M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.54M)
        {
            if (SEMC13 <= LB2C89)
            {
                if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.5499M, 4.5499M, basicTrigger, StartRule_4_5499);
                }
                else if (SEMC13 > SEMC21)
                {
                    return CreateTrigger(context, 4.5498M, 4.5498M, basicTrigger);
                }
                else if ((MAL2 < SEMC1 && MAL2 < SEMC2) || (SEMC1 > UBC1))
                {
                    return CreateTrigger(context, 4.5449M, -4.5449M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.5419M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 > MAC1 && SEMC4 > MAC4 && SEMC5 > MAC5)
            {
                if (((MAL2 > MAL1 && MAL2 > MAC1) || (MAL2 > MAC1 && MAL2 < UBC2)) && (MAL2 > MAL1 || MAL2 > SEMC3 || MAL2 < UBC2) && SEMC4 > SEMC5 && (SEMC1 < SEMC2 || SEMC1 < UBC1))
                {
                    return CreateTrigger(context, 4.5497M, 4.5497M, basicTrigger);
                }
                else if (SEMC4 < SEMC5 && SEMC4 > UBC5 && SEMC5 > UBC5)
                {
                    return CreateTrigger(context, 4.5448M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.5418M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 < MAC1)
            {
                if (MAC1 < UBC5)
                {
                    return CreateTrigger(context, 4.5496M, 4.5496M, basicTrigger, StartRule_4_5496);
                }
                else if (SEMC4 < SEMC5 && MAL2 < SEMC3 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.5447M, -4.5447M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.5417M, 0M, basicTrigger);
                }
            }
            else if ((MAL2 < SEMC1 && MAL2 < SEMC2 && MAL2 < MAL1) || (SEMC4 < SEMC5 && MAL2 < SEMC1 && MAL2 < SEMC2))
            {
                return CreateTrigger(context, 4.5446M, -4.5446M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.5416M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.53M)
        {
            return CreateTrigger(context, 4.5399M, 4.5399M, basicTrigger);
        }

        if (basicTrigger == 4.52M)
        {
            return CreateTrigger(context, 4.5299M, 4.5299M, basicTrigger);
        }

        if (basicTrigger == 4.49M)
        {
            if (UBL2 > MAC2)
            {
                return CreateTrigger(context, 4.4946M, -4.4946M, basicTrigger);
            }
            else if (SEMC13 < LB2C89 || MAC5 < LB2C89)
            {
                if (SEMC2 < MAC2 && MAL2 > MAL1 && MAL2 > MAC3 && MAL2 > MAC1 && SEMC1 > MAC1 && SEMC5 > SEMC8)
                {
                    return CreateTrigger(context, 4.4997M, 4.4997M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4919M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > SEMC1 && MAL2 > MAC1)
            {
                if (C5_SEMTrend == -0.1M && SEMC5 > UBC5 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < SEMC3 && MAL2 < SEMC5)
                {
                    return CreateTrigger(context, 4.4949M, -4.4949M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4999M, 4.4999M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5 && C5_SEMTrend == -0.1M && SEMC2 < MAC2 && MAL2 > MAL1 && MAL2 > MAC3 && MAL2 > MAC1 && SEMC1 > MAC1 && SEMC1 < UBC1 && MAC5 > MAC8 && SEMC5 > SEMC8)
            {
                return CreateTrigger(context, 4.4998M, 4.4998M, basicTrigger);
            }
            else if (SEMC5 > UBC5 && C5_SEMTrend == -0.1M && MAL2 < MAC2 && MAL2 < MAC3 && SEMC1 < UBC1)
            {
                return CreateTrigger(context, 4.4948M, -4.4948M, basicTrigger);
            }
            else if (SEMC5 > UBC5 && C5_SEMTrend == -0.1M && MAL2 < MAC2 && (MAL2 > MAC3 || SEMC1 > UBC1))
            {
                return CreateTrigger(context, 4.4947M, -4.4947M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.4919M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.48M)
        {
            if (MAC2 > MAC3)
            {
                if (SEMC1 < UBC1)
                {
                    return CreateTrigger(context, 4.4899M, 4.4899M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4849M, -4.4849M, basicTrigger);
                }
            }
            else if (SEMC8 < LBC4 && SEMC5 < LBC3)
            {
                return CreateTrigger(context, 4.4898M, 4.4898M, basicTrigger);
            }
            else if (SEMC5 < LBC3)
            {
                return CreateTrigger(context, 4.4897M, 4.4897M, basicTrigger);
            }
            else if (SEMC3 < LB2C5 && SEMC2 < LB2C3 && SEMC1 < LB2C2)
            {
                return CreateTrigger(context, 4.4896M, 0M, basicTrigger);
            }
            else if (MAL2 > MAC1 && MAL2 > SEMC1 && C1_SEMTrend != -0.1M)
            {
                if (SEML2 < MAL2 && SEML1 < MAL2)
                {
                    return CreateTrigger(context, 4.4819M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4849M, -4.4849M, basicTrigger);
                }
            }
            else if (C1_SEMTrend != -0.1M && MAL2 < SEMC1)
            {
                return CreateTrigger(context, 4.4848M, -4.4848M, basicTrigger);
            }
            else if (MAL2 < MAC1)
            {
                return CreateTrigger(context, 4.4847M, -4.4847M, basicTrigger);
            }
            else if (MAL2 < SEMC1 && SEMC1 > UBC1)
            {
                return CreateTrigger(context, 4.4846M, -4.4846M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.4818M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.47M)
        {
            if (SEMC3 < LB2C5)
            {
                return CreateTrigger(context, 4.4799M, 4.4799M, basicTrigger);
            }
            else if (MAL2 > LB2C8 && MAL2 > MAC21)
            {
                return CreateTrigger(context, 4.4798M, 4.4798M, basicTrigger, StartRule_4_4798);
            }
            else if (SEMC5 < MAC1)
            {
                return CreateTrigger(context, 4.4797M, 4.4797M, basicTrigger, StartRule_4_4797);
            }
            else if (SEML2 > MAL2)
            {
                return CreateTrigger(context, 4.4749M, -4.4749M, basicTrigger);
            }
            else if (MAC2 < MAC3 && SEMC8 > LBC4 && SEMC5 > LBC3)
            {
                return CreateTrigger(context, 4.4748M, -4.4748M, basicTrigger);
            }
            else if (SEMC8 > MAC2 && SEMC8 > LBC4 && SEMC5 < LBC3)
            {
                return CreateTrigger(context, 4.4747M, -4.4747M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.4719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.43M)
        {
            if (SEMC3 > MAC3)
            {
                if (SEMC1 < LBC1)
                {
                    return CreateTrigger(context, 4.4399M, 4.4399M, basicTrigger);
                }
                else if (SEMC1 > LBC1 && SEMC2 > LBC1)
                {
                    return CreateTrigger(context, 4.4349M, -4.4349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4319M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 <= MAC3)
            {
                if (SEMC2 > LBC1 && MAL2 < MAC5)
                {
                    return CreateTrigger(context, 4.4348M, -4.4348M, basicTrigger);
                }
                else if (SEMC1 > LBC1 && SEMC2 > LBC1)
                {
                    return CreateTrigger(context, 4.4318M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.42M)
        {
            if (UBC1 < LBC5)
            {
                if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 4.4299M, 4.4299M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4249M, -4.4249M, basicTrigger);
                }
            }
            else if (MAC1 > MAC5)
            {
                return CreateTrigger(context, 4.4298M, 4.4298M, basicTrigger);
            }
            else if (SEMC3 > SEMC13)
            {
                return CreateTrigger(context, 4.4219M, 0M, basicTrigger);
            }
            else if (MAL2 < MAC1 || (MAL2 < SEMC13 && SEMC1 > UBC1))
            {
                return CreateTrigger(context, 4.4248M, -4.4248M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.4218M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.41M)
        {
            if (SEMC1 >= MAC1)
            {
                if (MAC1 < LB2C89)
                {
                    if (SEMC5 > SEMC13 || SEMC8 > SEMC13 || SEMC5 > SEMC21)
                    {
                        return CreateTrigger(context, 4.4199M, 4.4199M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.4149M, 0M, basicTrigger);
                    }
                }
                else if (UBC1 < LBC5 && LBC5 < LBC13)
                {
                    return CreateTrigger(context, 4.4198M, 4.4198M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4119M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 < MAC1)
            {
                if (MAC1 < LB2C89)
                {
                    return CreateTrigger(context, 4.4148M, -4.4148M, basicTrigger);
                }
                else if (UBL2 < LB2C5)
                {
                    return CreateTrigger(context, 4.4197M, 0M, basicTrigger);
                }
                else if (MAL3 > MAL2)
                {
                    return CreateTrigger(context, 4.4196M, 4.4196M, basicTrigger);
                }
                else if (UBC1 < LBC5 && SEMC1 > LBC1 && (SEMC1 > SEMC2 || SEMC1 > SEMC3))
                {
                    if (SEML2 > SEMC2 || SEML2 > SEMC3)
                    {
                        return CreateTrigger(context, 4.4195M, 4.4195M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.4118M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 < LBC1 && MAL2 < MAL1 && MAL3 < MAL2)
                {
                    return CreateTrigger(context, 4.4147M, -4.4147M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4117M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.39M)
        {
            if (MAL2 < SEMC21)
            {
                if (SEMC5 > UBC5)
                {
                    return CreateTrigger(context, 4.3999M, 4.3999M, basicTrigger);
                }
                else if (SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 4.3949M, -4.3949M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3919M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < MAC5)
            {
                if (SEMC1 > SEMC2 && SEMC2 > SEMC3 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && SEMC4 > SEMC8 && MAL2 > SEMC3 && MAL2 > MAL1 && MAC1 > MAC5 && SEMC3 > MAC5)
                {
                    return CreateTrigger(context, 4.3998M, 4.3998M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3918M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5)
            {
                if (SEMC1 < MAC1)
                {
                    if (SEMC3 > SEMC4 || SEMC3 > SEMC5)
                    {
                        return CreateTrigger(context, 4.3997M, 4.3997M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3917M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > SEMC5 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5)
                {
                    return CreateTrigger(context, 4.3996M, 4.3996M, basicTrigger);
                }
                else if ((MAL2 < SEMC1 && MAL2 < SEMC2 && MAL2 < SEMC3) || (MAL2 < SEMC1 && MAL2 < SEMC5))
                {
                    return CreateTrigger(context, 4.3948M, -4.3948M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3916M, 0M, basicTrigger);
                }
            }
            else if ((SEMC4 < SEMC5 && MAL2 < SEMC1 && SEMC1 > MAC1) || (SEMC1 > UBC1) || (MAL2 < SEMC1 && MAL2 < MAL1))
            {
                return CreateTrigger(context, 4.3947M, -4.3947M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.3915M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.37M)
        {
            if (SEMC8 < SEMC13 && SEMC8 < LBC21 && MAC5 < MAC21)
            {
                return CreateTrigger(context, 4.3749M, -4.3749M, basicTrigger, StartRule_4_3749);
            }
            else
            {
                return CreateTrigger(context, 4.3719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.36M)
        {
            return CreateTrigger(context, 4.3699M, 4.3699M, basicTrigger);
        }

        if (basicTrigger == 4.35M)
        {
            if (MAL2 > UBC5 && SEMC5 > UBC5)
            {
                return CreateTrigger(context, 4.3599M, 4.3599M, basicTrigger);
            }
            else if (MAL2 > UBC5 && SEMC4 > UBC5)
            {
                return CreateTrigger(context, 4.3598M, 4.3598M, basicTrigger, StartRule_4_3598);
            }
            else if ((MAC1 < UBC5 && MAL2 < SEMC1 && MAL2 < SEMC2) || (MAC1 < UBC5 && SEMC1 > UBC1) || (MAC1 < UBC5 && MAL2 < MAL1) || (MAC1 < UBC5 && SEMC3 < SEMC5))
            {
                return CreateTrigger(context, 4.3549M, -4.3549M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.3519M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.34M)
        {
            if (MAL2 > UBC5 && MAL2 < SEMC21 && SEMC13 < LBC13)
            {
                if (MAC1 > MAC21 && MAC1 > UBC5 && MAC1 > UBC4 && SEMC1 < UBC1)
                {
                    return CreateTrigger(context, 4.3499M, 4.3499M, basicTrigger, StartRule_4_3499);
                }
                else if (SEMC4 > UBC4)
                {
                    return CreateTrigger(context, 4.3498M, 4.3498M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3449M, -4.3449M, basicTrigger, additionalStopCondition: StopRule_Negative_4_3449);
                }
            }
            else if (MAL2 > UBC5)
            {
                return CreateTrigger(context, 4.3497M, 4.3497M, basicTrigger);
            }
            else if (MAC1 > UBC5 && SEMC1 < MAC1 && SEMC1 > SEMC5)
            {
                return CreateTrigger(context, 4.3496M, 4.3496M, basicTrigger, StartRule_4_3496);
            }
            else if (SEMC21 > UBC8)
            {
                if (SEMC3 > MAC3 && MAL2 < MAL1 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.3448M, -4.3448M, basicTrigger);
                }
                else if (MAL2 < SEMC13)
                {
                    return CreateTrigger(context, 4.3447M, -4.3447M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3419M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 < MAC3 && MAL2 < MAC3)
            {
                if (MAL2 >= MAC5)
                {
                    return CreateTrigger(context, 4.3495M, 4.3495M, basicTrigger);
                }
                else if (MAL2 < MAC5)
                {
                    if (SEMC1 > SEMC3 || SEMC2 > SEMC3 || MAL1 > SEMC3 || MAL2 > SEMC3)
                    {
                        return CreateTrigger(context, 4.3494M, 4.3494M, basicTrigger, StartRule_4_3494);
                    }
                    else if (SEMC1 < SEMC3 && SEMC2 < SEMC3 && MAL1 < SEMC3 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 4.3446M, -4.3446M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3418M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC2 < MAC2 && MAL2 < MAC2)
            {
                if (MAL2 >= MAC5)
                {
                    return CreateTrigger(context, 4.3493M, 4.3493M, basicTrigger);
                }
                else if (MAL2 < MAC5)
                {
                    if (SEMC1 > SEMC2 || SEMC1 > SEMC3 || MAL2 > SEMC2)
                    {
                        return CreateTrigger(context, 4.3492M, 4.3492M, basicTrigger);
                    }
                    else if (SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 4.3445M, -4.3445M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3417M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC3 > MAC3 && SEMC2 > MAC2 && ((MAL2 < SEMC3 && MAL2 < SEMC2) || (MAL2 < SEMC2 && MAL2 < SEMC1)))
            {
                return CreateTrigger(context, 4.3444M, -4.3444M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.3416M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.33M)
        {
            if (SEMC13 < C4_C12_Lower && SEMC3 < SEMC13)
            {
                if (SEMC5 < SEMC13 && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 4.3349M, -4.3349M, basicTrigger, StartRule_4_3349, additionalStopCondition: StopRule_Negative_4_3349);
                }
                else
                {
                    return CreateTrigger(context, 4.3319M, 0M, basicTrigger);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.3399M, 4.3399M, basicTrigger, StartRule_4_3399);
            }
        }

        if (basicTrigger == 4.32M)
        {
            if (SEMC5 < MAC5 && MAL2 < MAC5 && (SEMC3 > MAC3 || SEMC4 > MAC4 || SEMC2 > MAC2))
            {
                return CreateTrigger(context, 4.3289M, 4.3289M, basicTrigger);
            }
            else if (MAL2 < UBC8)
            {
                if (MAC2 > UBC8 && MAC1 > UBC8)
                {
                    if (SEMC2 > MAC2)
                    {
                        return CreateTrigger(context, 4.3293M, 4.3293M, basicTrigger);
                    }
                    else if (SEMC2 < MAC2 && SEMC2 > UBC8)
                    {
                        return CreateTrigger(context, 4.3292M, 4.3292M, basicTrigger, StartRule_4_3292, additionalStopCondition: StopRule_4_3292);
                    }
                    else if (SEMC2 < MAC2 && SEMC2 < UBC8)
                    {
                        return CreateTrigger(context, 4.3244M, -4.3244M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3219M, 0M, basicTrigger);
                    }
                }
                else if (MAC2 > UBC8 || MAC1 > UBC8)
                {
                    if ((SEMC2 < MAC2 && SEMC2 < UBC8) || (C5_SEMTrend == -0.1M && C4_SEMTrend == -0.1M && C3_SEMTrend == -0.1M && C2_SEMTrend == -0.1M) || (SEMC1 < MAC1 && MAC1 > UBC8 && SEMC3 < SEMC5 && SEMC4 < SEMC5))
                    {
                        return CreateTrigger(context, 4.3243M, -4.3243M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3218M, 0M, basicTrigger);
                    }
                }
                else if (C5_SEMTrend != -0.1M)
                {
                    if (MAL2 > SEMC5 && (SEMC2 > MAC2 || SEMC3 > MAC3 || SEMC4 > MAC4))
                    {
                        return CreateTrigger(context, 4.3291M, 4.3291M, basicTrigger, StartRule_4_3291);
                    }
                    else if (SEMC5 < MAC5 && MAL2 < SEMC5 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC4 < MAC4 && SEMC3 < SEMC5 && SEMC3 < SEMC4)
                    {
                        return CreateTrigger(context, 4.3242M, -4.3242M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3217M, 0M, basicTrigger);
                    }
                }
                else if ((SEMC3 < UBC8 && SEMC3 > MAC3 && SEMC3 < SEMC5) || (SEMC4 < UBC8 && SEMC4 > MAC3 && SEMC4 < SEMC5 && SEMC3 < MAC3) || (SEMC5 < UBC8 && SEMC5 > MAC3 && SEMC3 < MAC3 && SEMC4 < MAC4))
                {
                    return CreateTrigger(context, 4.3241M, -4.3241M, basicTrigger);
                }
                else if ((SEMC3 < UBC8 || SEMC4 < UBC8) && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 4.3238M, -4.3238M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3216M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 > UBC13 && SEMC8 > UBC8 && MAL2 > SEMC8 && MAL2 > SEMC13)
            {
                return CreateTrigger(context, 4.3299M, 4.3299M, basicTrigger);
            }
            else if (SEMC8 > UBC8 && SEMC5 > UBC5 && MAC1 > UBC8 && MAL2 > UBC8 && MAC1 < UBC5 && MAL2 < UBC5 && SEMC5 > SEMC8)
            {
                return CreateTrigger(context, 4.3298M, 4.3298M, basicTrigger, StartRule_4_3298);
            }
            else if (C3_SEMTrend != -0.1M && SEMC3 < UBC3 && MAL2 > SEMC8 && MAL2 > SEMC3 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.3297M, 4.3297M, basicTrigger, StartRule_4_3297);
            }
            else if (C5_SEMTrend != -0.1M && SEMC5 < UBC5 && MAL2 > SEMC8 && MAL2 > SEMC5 && MAL2 > UBC5 && SEMC1 > MAC1 && MAL2 > MAC1)
            {
                return CreateTrigger(context, 4.3296M, 4.3296M, basicTrigger, StartRule_4_3296);
            }
            else if (MAL2 >= MAC21)
            {
                if (SEMC8 > UBC8 && SEMC5 > UBC5)
                {
                    if (SEMC5 > SEMC8 && SEMC4 > SEMC5 && SEMC3 > SEMC5 && MAL1 > MAC1 && MAL2 > MAC1)
                    {
                        return CreateTrigger(context, 4.3295M, 4.3295M, basicTrigger, StartRule_4_3295);
                    }
                    else if (SEMC5 > SEMC8 && MAC1 < UBC5 && SEMC1 > MAC1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAL1 > MAC1 && MAL2 > MAC1)
                    {
                        return CreateTrigger(context, 4.3294M, 4.3294M, basicTrigger, StartRule_4_3294);
                    }
                    else if (MAC1 > UBC5 && SEMC1 > MAC1 && MAL2 > UBC8 && MAC3 < UBC5 && C5_SEMTrend == -0.1M && C4_SEMTrend == -0.1M && C3_SEMTrend == -0.1M && (C2_SEMTrend == -0.1M || C2_SEMTrend == 1.1M) && (SEMC5 < SEMC8 || SEMC4 < SEMC5 || SEMC3 < SEMC5 || MAL1 < MAC1 || MAL2 < MAC1))
                    {
                        return CreateTrigger(context, 4.3249M, -4.3249M, basicTrigger);
                    }
                    else if (MAC1 > UBC5 && MAL2 > UBC8 && MAC3 < UBC5 && (SEMC5 < SEMC8 || SEMC4 < SEMC5 || SEMC3 < SEMC5 || MAL1 < MAC1 || MAL2 < MAC1))
                    {
                        return CreateTrigger(context, 4.3248M, -4.3248M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3215M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 > UBC8)
                {
                    if (MAC1 > UBC5 && SEMC1 > MAC1 && MAL2 > UBC5 && MAL2 > UBC8 && MAC3 < UBC5 && C5_SEMTrend == -0.1M && C4_SEMTrend == -0.1M && C3_SEMTrend == -0.1M && (C2_SEMTrend == -0.1M || C2_SEMTrend == 1.1M) && (SEMC5 < SEMC8 || SEMC4 < SEMC5 || SEMC3 < SEMC5 || MAL1 < MAC1 || MAL2 < MAC1))
                    {
                        return CreateTrigger(context, 4.3247M, -4.3247M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.3214M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.3213M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC21)
            {
                if (MAC1 > UBC5 && MAL2 > UBC8 && MAC3 < UBC5 && C5_SEMTrend == -0.1M && C4_SEMTrend == -0.1M && C3_SEMTrend == -0.1M && (C2_SEMTrend == -0.1M || C2_SEMTrend == 1.1M) && (SEMC5 < SEMC8 || SEMC4 < SEMC5 || SEMC3 < SEMC5 || MAL1 < MAC1 || MAL2 < MAC1))
                {
                    return CreateTrigger(context, 4.3246M, -4.3246M, basicTrigger);
                }
                else if (MAC1 > UBC5 && MAL2 > UBC8 && MAC3 < UBC5 && (SEMC5 < SEMC8 || SEMC4 < SEMC5 || SEMC3 < SEMC5 || MAL1 < MAC1 || MAL2 < MAC1))
                {
                    return CreateTrigger(context, 4.3245M, -4.3245M, basicTrigger);
                }
                else if (MAC1 < UBC8 && MAC2 > UBC8)
                {
                    return CreateTrigger(context, 4.3239M, -4.3239M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.3212M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.31M)
        {
            if (MAL2 > SEMC13 && SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC8 > SEMC21 && SEMC5 > SEMC21)
            {
                return CreateTrigger(context, 4.3199M, 4.3199M, basicTrigger);
            }
            else if (MAL2 < SEMC13 && ((MAL2 < MAC1 && MAL2 < SEMC3) || (MAL2 < MAC1 && MAL2 < SEMC5)))
            {
                return CreateTrigger(context, 4.3149M, -4.3149M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.3119M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.29M)
        {
            if (MAL2 > UBC8)
            {
                if (SEMC5 > SEMC8 && SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC8 > SEMC21 && SEMC5 > SEMC21)
                {
                    return CreateTrigger(context, 4.2999M, 4.2999M, basicTrigger);
                }
                else if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && C5_SEMTrend == -0.1M && C4_SEMTrend == -0.1M && C3_SEMTrend == -0.1M && C2_SEMTrend == -0.1M && MAL2 < SEMC8 && MAL2 < SEMC5 && (MAL2 < SEMC13 || MAL2 < SEMC21))
                {
                    return CreateTrigger(context, 4.2949M, -4.2949M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2919M, 0M, basicTrigger);
                }
            }
            else if (MAL2 <= UBC8)
            {
                if (SEMC8 > SEMC13 && SEMC8 > SEMC21 && SEMC5 > SEMC13 && SEMC5 > SEMC21 && SEMC3 > SEMC13 && SEMC3 > SEMC21 && (MAL2 > SEMC13 || MAL2 > SEMC21))
                {
                    return CreateTrigger(context, 4.2998M, 4.2998M, basicTrigger, StartRule_4_2998);
                }
                else if (MAL2 < SEMC2 && SEMC2 > LBC2 && (MAL2 < SEMC13 || MAL2 < SEMC21))
                {
                    return CreateTrigger(context, 4.2948M, -4.2948M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2918M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.28M)
        {
            if (SEMC5 >= UBC5)
            {
                if (C5_SEMTrend != -0.1M && MAL2 > SEMC5 && MAC1 > MAC21 && (SEMC3 > SEMC5 || SEMC3 > SEMC4 || SEMC4 > SEMC5) && MAL2 > MAC1 && MAL1 > MAC1 && MAL2 > SEMC5)
                {
                    return CreateTrigger(context, 4.2899M, 4.2899M, basicTrigger, StartRule_4_2899);
                }
                else if (((C5_SEMTrend == -0.1M) || (MAL2 < SEMC5)) && SEMC3 < SEMC5 && SEMC4 < SEMC5)
                {
                    return CreateTrigger(context, 4.2849M, -4.2849M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2819M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5)
            {
                if (SEML2 > MAL2 && SEML1 > MAL1 && SEML2 < SEMC1 && SEML1 < SEMC1 && MAL2 < SEMC1 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.2848M, -4.2848M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2818M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.27M)
        {
            return CreateTrigger(context, 4.2799M, 4.2799M, basicTrigger, StartRule_4_2799, additionalStopCondition: StopRule_4_2799);
        }

        if (basicTrigger == 4.25M)
        {
            if (MAC3 >= MAC5)
            {
                if (SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC3 > SEMC13)
                {
                    return CreateTrigger(context, 4.2599M, 4.2599M, basicTrigger);
                }
                else if (SEMC8 > SEMC13 || SEMC5 > SEMC13 || SEMC3 > SEMC13)
                {
                    return CreateTrigger(context, 4.2598M, 4.2598M, basicTrigger);
                }
                else if (SEMC8 < SEMC13 && SEMC5 < SEMC13 && SEMC3 < SEMC13)
                {
                    return CreateTrigger(context, 4.2549M, -4.2549M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2519M, 0M, basicTrigger);
                }
            }
            else if (MAC3 < MAC5)
            {
                if ((MAL2 < SEMC1 && MAL2 < SEMC2) || (MAL2 < SEMC1 && SEMC1 > UBC1))
                {
                    return CreateTrigger(context, 4.2548M, -4.2548M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2518M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.19M)
        {
            if (C3_SEMTrend != -0.1M && SEMC3 < UBC3 && MAL2 > SEMC8 && MAL2 > SEMC3 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.1999M, 4.1999M, basicTrigger);
            }
            else if (C2_SEMTrend != -0.1M && SEMC2 < UBC2 && MAL2 > SEMC8 && MAL2 > SEMC2 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.1998M, 4.1998M, basicTrigger);
            }
            else if (C5_SEMTrend == 1.1M && C4_SEMTrend == 1.1M && MAL2 > SEMC8 && MAL2 > SEMC5 && MAL2 > SEMC4 && SEMC2 > SEMC3)
            {
                return CreateTrigger(context, 4.1997M, 4.1997M, basicTrigger);
            }
            else if (MAC2 > UBC8 && MAC1 > UBC8)
            {
                if (SEMC2 < MAC2 && SEMC2 > UBC8)
                {
                    return CreateTrigger(context, 4.1996M, 4.1996M, basicTrigger);
                }
                else if (SEMC1 < MAC1 && SEMC1 > UBC5)
                {
                    return CreateTrigger(context, 4.1995M, 4.1995M, basicTrigger);
                }
                else if (MAL2 < MAC1 && SEMC1 > UBC5 && MAL2 > SEMC8 && C8_SEMTrend == 1.1M && SEMC2 > SEMC3 && SEMC3 > SEMC5)
                {
                    return CreateTrigger(context, 4.1994M, 4.1994M, basicTrigger);
                }
                else if (MAC1 < MAC2 && SEMC2 < UBC8 && SEMC1 < UBC8)
                {
                    return CreateTrigger(context, 4.1949M, -4.1949M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && SEMC2 > MAC2 && MAL2 > MAC1 && MAL1 > MAC1 && SEMC2 < SEMC5 && SEMC1 < SEMC4 && (MAL2 < SEMC1 || MAL2 < SEMC2))
                {
                    return CreateTrigger(context, 4.1948M, -4.1948M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && SEMC2 > MAC2 && MAL2 > MAC1 && MAL1 > MAC1 && MAL2 < SEMC1 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.1947M, -4.1947M, basicTrigger);
                }
                else if (SEMC1 < MAC1 && SEMC1 < UBC5 && SEMC2 > MAC2 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 4.1946M, -4.1946M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1919M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.1918M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.14M)
        {
            if (C5_UBLB2_Dir == 1 && MAL2 > SEMC5 && MAL2 > LBC5)
            {
                if (SEMC1 <= UBC1 && SEMC2 > SEMC5 && SEMC2 > SEMC3)
                {
                    return CreateTrigger(context, 4.1499M, 4.1499M, basicTrigger);
                }
                else if (SEMC1 > UBC1 || SEML1 > UBC1)
                {
                    return CreateTrigger(context, 4.1449M, -4.1449M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1419M, 0M, basicTrigger);
                }
            }
            else if (C5_UBLB2_Dir == -1)
            {
                return CreateTrigger(context, 4.1448M, -4.1448M, basicTrigger);
            }
            else if (MAL2 < SEMC5)
            {
                return CreateTrigger(context, 4.1447M, -4.1447M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.1418M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.07M)
        {
            if (((MAL2 > SEMC5 && MAL2 > MAC5) || MAC1 > MAC5) && MAL2 > MAC1 && SEMC2 > SEMC5)
            {
                return CreateTrigger(context, 4.0799M, 4.0799M, basicTrigger);
            }
            else if (MAL2 < SEMC5 && MAC1 < MAC5 && SEMC1 < MAC1 && MAL2 < MAC1)
            {
                return CreateTrigger(context, 4.0749M, -4.0749M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.08M)
        {
            if (MAL2 > SEMC8)
            {
                if (MAL2 > SEMC5 && MAL2 > SEMC4 && SEMC2 > SEMC4)
                {
                    return CreateTrigger(context, 4.0899M, 4.0899M, basicTrigger, StartRule_4_0899);
                }
                else if (MAL2 > SEMC5)
                {
                    return CreateTrigger(context, 4.0898M, 4.0898M, basicTrigger, StartRule_4_0898);
                }
                else
                {
                    return CreateTrigger(context, 4.0819M, 0M, basicTrigger);
                }
            }
            else if (MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && MAC4 < MAC5 && MAL2 < MAC1 && (MAL2 < MAC2 || MAL2 < MAC3 || MAL2 < MAC4 && SEMC1 < MAC2 && SEMC1 < MAC3 && SEMC1 < MAC4 && SEMC1 < LBC1 && SEMC2 > MAC2 && SEMC3 > MAC3) && SEMC2 > SEMC5 && SEMC3 > SEMC5 && MAL2 > MAC13 && MAL2 > MAC21)
            {
                return CreateTrigger(context, 4.0897M, 4.0897M, basicTrigger);
            }
            else if (MAL2 < LB2C5 && MAL2 < LB2C8 && MAL2 < LB2C4 && SEMC3 < LBC3)
            {
                return CreateTrigger(context, 4.0896M, 4.0896M, basicTrigger);
            }
            else if (MAL2 < SEMC8 && MAC1 < MAC3 && MAC2 < MAC3 && MAL2 < LBC8)
            {
                return CreateTrigger(context, 4.0849M, -4.0849M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0818M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.05M)
        {
            if (C13_UBLB1_Dir == 1)
            {
                if (SEMC8 > MAC8 && SEMC13 < MAC13 && MAL2 > SEMC2 && MAL2 > LBC2)
                {
                    return CreateTrigger(context, 4.0599M, 4.0599M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.0519M, 0M, basicTrigger);
                }
            }
            else if (C13_UBLB1_Dir == -1)
            {
                if (SEMC4 < MAC4)
                {
                    if (MAC5 > UBC13 && MAC3 > UBC13)
                    {
                        return CreateTrigger(context, 4.0598M, 4.0598M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.0518M, 0M, basicTrigger);
                    }
                }
                else if (MAC2 < MAC4 && SEMC4 < MAC3 && UBC1 < MAC2)
                {
                    if (MAC5 > UBC13 && MAC3 > UBC13)
                    {
                        return CreateTrigger(context, 4.0597M, 4.0597M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.0517M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 4.0549M, -4.0549M, basicTrigger);
                }
                else if (SEMC4 > MAC4)
                {
                    return CreateTrigger(context, 4.0548M, -4.0548M, basicTrigger, StartRule_4_0548);
                }
                else
                {
                    return CreateTrigger(context, 4.0516M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.06M)
        {
            if (SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC4 > MAC4)
            {
                if ((MAL2 > MAC1 || SEMC1 > MAC1) && SEMC1 < SEMC13 && SEMC2 < SEMC13)
                {
                    return CreateTrigger(context, 4.0699M, 4.0699M, basicTrigger, StartRule_4_0699);
                }
                else
                {
                    return CreateTrigger(context, 4.0619M, 0M, basicTrigger);
                }
            }
            else if ((SEMC2 < MAC2 || SEMC3 < MAC3 || SEMC4 < MAC4) && MAL2 < MAC1)
            {
                return CreateTrigger(context, 4.0649M, -4.0649M, basicTrigger);
            }
            else if (SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC4 < MAC4 && MAL2 < SEMC5)
            {
                return CreateTrigger(context, 4.0648M, -4.0648M, basicTrigger);
            }
            else if (SEMC2 < MAC2 && SEMC3 < MAC3 && MAL2 < SEMC4)
            {
                return CreateTrigger(context, 4.0647M, -4.0647M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0618M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.02M)
        {
            if (MAL2 < MAC8)
            {
                return CreateTrigger(context, 4.0249M, -4.0249M, basicTrigger);
            }
            else if (MAL2 >= MAC8 && SEMC1 < LBC1 && MAL2 < LBC5 && MAL2 < MAL1)
            {
                return CreateTrigger(context, 4.0299M, 4.0299M, basicTrigger, additionalStopCondition: StopRule_4_0299);
            }
            else if (MAL2 > LBC5 && SEMC1 < SEMC5 && SEMC1 < MAC1)
            {
                return CreateTrigger(context, 4.0248M, -4.0248M, basicTrigger, StartRule_4_0248);
            }
            else if (MAL2 > LBC5 && MAL2 < SEMC5 && SEMC1 < SEMC5 && SEMC1 > MAC1)
            {
                return CreateTrigger(context, 4.0247M, -4.0247M, basicTrigger, StartRule_4_0247);
            }
            else
            {
                return CreateTrigger(context, 4.0219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.01M)
        {
            if (SEMC2 < LB2C4 && SEMC3 < LBC2)
            {
                return CreateTrigger(context, 4.0199M, 4.0199M, basicTrigger, additionalStopCondition: StopRule_4_0199);
            }
            else if (C13_SEMTrend != -0.1M && SEMC13 > SEMC21)
            {
                if (SEMC5 > MAC5 && SEMC4 > MAC4 && SEMC3 > MAC3)
                {
                    return CreateTrigger(context, 4.0149M, -4.0149M, basicTrigger);
                }
                else if (SEMC5 > MAC5 && SEMC4 > MAC4)
                {
                    return CreateTrigger(context, 4.0148M, -4.0148M, basicTrigger);
                }
                else if (SEMC5 > MAC5)
                {
                    return CreateTrigger(context, 4.0147M, -4.0147M, basicTrigger);
                }
                else if (SEMC5 <= MAC5)
                {
                    if (SEMC2 < LB2C4)
                    {
                        return CreateTrigger(context, 4.0198M, 4.0198M, basicTrigger);
                    }
                    else if (MAC1 < LBC5)
                    {
                        return CreateTrigger(context, 4.0146M, -4.0146M, basicTrigger, StartRule_4_0146);
                    }
                    else if (C5_UBLB2_Dir == 1)
                    {
                        return CreateTrigger(context, 4.0197M, 4.0197M, basicTrigger, StartRule_4_0197);
                    }
                    else if (MAL2 < SEMC5)
                    {
                        return CreateTrigger(context, 4.0145M, -4.0145M, basicTrigger, StartRule_4_0145);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.0119M, 0M, basicTrigger);
                    }
                }
            }
            else if (C5_UBLB2_Dir == 1 && MAC1 < LBC5)
            {
                if (MAL2 > SEMC3)
                {
                    return CreateTrigger(context, 4.0118M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.0144M, -4.0144M, basicTrigger);
                }
            }
            else if (MAL2 > SEMC3)
            {
                return CreateTrigger(context, 4.0143M, -4.0143M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0142M, -4.0142M, basicTrigger);
            }
        }

        if (basicTrigger == 1.99M)
        {
            if (C5_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 1.9999M, 1.9999M, basicTrigger);
            }
            else if (SEMC2 < UBC1 && SEMC1 > UBC1 && MAL3 < MAL2 && SEMC1 > SEMC2)
            {
                return CreateTrigger(context, 1.9919M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 1.9949M, -1.9949M, basicTrigger);
            }
        }

        if (basicTrigger == 1.98M)
        {
            if (C8_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 1.9899M, 1.9899M, basicTrigger);
            }
            else if (SEMC5 < UBC5 && MAC1 < UBC5 && (C13_UBLB2_Dir == 1 || C21_UBLB2_Dir == 1))
            {
                return CreateTrigger(context, 1.9898M, 1.9898M, basicTrigger);
            }
            else if (SEMC13 > UBC13 && SEMC8 > UBC8 && LB2C3 < MAC5 && LB2C2 > MAC5 && SEMC5 > UBC5 && LBC1 > UBC5 && UB2C13 > UB2C21 && MAL2 > SEMC13)
            {
                return CreateTrigger(context, 1.9897M, 1.9897M, basicTrigger);
            }
            else if (LBC2 > UBC5 && MAC1 > MAC2)
            {
                return CreateTrigger(context, 1.9896M, 1.9896M, basicTrigger);
            }
            else if (MAC2 > UBC5 && SEMC2 < MAC1 && SEMC1 < MAC1 && SEMC1 < MAC2)
            {
                return CreateTrigger(context, 1.9898M, 1.9898M, basicTrigger);
            }
            else if (MAC2 > UBC5)
            {
                if (MAL2 > UBC2)
                {
                    if (SEMC1 > SEMC5 && (SEMC1 > SEMC2 || SEMC2 > SEMC3))
                    {
                        return CreateTrigger(context, 1.9895M, 1.9895M, basicTrigger);
                    }
                    else if ((SEMC1 < SEMC5 || (SEMC1 > SEMC2 && SEMC2 > SEMC3)) && MAL2 > UBC5 && SEMC1 > UBC5)
                    {
                        return CreateTrigger(context, 1.9849M, -1.9849M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 1.9819M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > UBC3)
                {
                    if (SEMC1 < UBC2 && SEML1 < UBC2 && SEML2 < UBC2 && SEMC8 > SEMC13)
                    {
                        return CreateTrigger(context, 1.9894M, 1.9894M, basicTrigger, additionalStopCondition: StopRule_1_9894);
                    }
                    else if ((SEMC1 > UBC2 || SEML1 > UBC2 || SEML2 > UBC2) && MAL2 > UBC5 && SEMC1 > UBC5)
                    {
                        return CreateTrigger(context, 1.9848M, -1.9848M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 1.9818M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > UBC5 && SEMC1 > UBC5)
                {
                    return CreateTrigger(context, 1.9847M, -1.9847M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9817M, 0M, basicTrigger);
                }
            }
            else if (LBC1 > UBC3)
            {
                if (SEML2 < MAL2)
                {
                    return CreateTrigger(context, 1.9846M, -1.9846M, basicTrigger, StartRule_1_9846);
                }
                else
                {
                    return CreateTrigger(context, 1.9845M, -1.9845M, basicTrigger);
                }
            }
            else if (MAC1 > LBC3 && SEMC1 < LBC1 && SEMC1 < UBC5 && LBC1 > UBC5 && MAL2 < MAL1 && MAL1 < MAC1)
            {
                return CreateTrigger(context, 1.9893M, 1.9893M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 1.9844M, -1.9844M, basicTrigger);
            }
        }

        if (basicTrigger == 1.97M)
        {
            if (SEMC5 < UBC5 && MAC1 < UBC5)
            {
                if (C2_UBLB2_Dir == -1)
                {
                    return CreateTrigger(context, 1.9749M, -1.9749M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9719M, 0M, basicTrigger);
                }
            }
            else if (L1_C2_S_Cont_MAC == -1)
            {
                if ((SEMC5 < UBC5 || SEMC3 < UBC5) && MAC1 > UBC5)
                {
                    return CreateTrigger(context, 1.9799M, 1.9799M, basicTrigger);
                }
                else if (MAC1 > UBH2 && MAL1 > SEMC3 && SEML2 > MAL2)
                {
                    return CreateTrigger(context, 1.9798M, 1.9798M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9748M, -1.9748M, basicTrigger);
                }
            }
            else if (L1_C2_S_Cont_SEM == 0)
            {
                if (SEMC2 < UBC1 && MAC1 > UBH2 && MAL1 > UBC2 && (MAL2 > SEMC3 || MAL2 > SEMC4))
                {
                    return CreateTrigger(context, 1.9797M, 1.9797M, basicTrigger);
                }
                else if (SEMC2 > UBC1 && MAC1 > UBH2 && MAL1 > UBC2 && (MAL2 > SEMC5 || MAL2 > SEMC4) && SEMC1 < SEMC2 && (MAC1 > SEMC4 || MAC1 > SEMC5))
                {
                    return CreateTrigger(context, 1.9796M, 1.9796M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9747M, -1.9747M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5 && MAC1 > UBC5)
            {
                if (SEMC4 > UBC4 && SEMC1 > SEMC2 && SEMC2 > SEMC3)
                {
                    return CreateTrigger(context, 1.9795M, 1.9795M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9746M, -1.9746M, basicTrigger);
                }
            }
            else if (SEMC5 >= UBC5 && MAC1 > UBH2 && MAL1 > UBC2)
            {
                if (MAC2 > UBC5 && SEMC5 > MAC1 && SEMC5 < UBC1 && SEML1 < UBC1 && SEML2 < UBC1)
                {
                    return CreateTrigger(context, 1.9794M, 1.9794M, basicTrigger);
                }
                else if (SEMC1 > SEMC2)
                {
                    return CreateTrigger(context, 1.9745M, -1.9745M, basicTrigger);
                }
                else if (SEMC1 <= SEMC2 && SEMC2 > UBC1 && MAL3 > MAL2 && MAL2 > MAL1 && SEML2 > MAL2 && SEML2 < SEML1 && SEML1 > MAL1 && SEML1 < SEMC1 && (SEML1 < UBC1 || SEML2 < UBC1))
                {
                    return CreateTrigger(context, 1.9793M, 1.9793M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9744M, -1.9744M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 1.9743M, -1.9743M, basicTrigger);
            }
        }

        if (basicTrigger == 1.96M)
        {
            if (C3_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 1.9619M, 1.9619M, basicTrigger);
            }
            else if (MAL2 < SEMC1 && MAL2 < SEMC2)
            {
                return CreateTrigger(context, 1.9649M, -1.9649M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 1.9618M, 1.9618M, basicTrigger);
            }
        }

        if (basicTrigger == 1.95M)
        {
            if (C1_UBLB2_Dir == 1 && LB2C1 > MAC2)
            {
                return CreateTrigger(context, 1.9519M, 0M, basicTrigger);
            }
            else if (SEMC13 > MAC2)
            {
                return CreateTrigger(context, 1.9549M, -1.9549M, basicTrigger);
            }
            else if (MAL2 < SEMC2)
            {
                if (cParam.C2UB1_C2MAC == 0 && MAC2 > UBC5)
                {
                    return CreateTrigger(context, 1.9599M, 1.9599M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9548M, -1.9548M, basicTrigger);
                }
            }
            else if (cParam.C2UB1_C2MAC == 0)
            {
                return CreateTrigger(context, 1.9598M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 1.9518M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 1.94M)
        {
            if (SEMC13 > UBC13 && SEMC8 > UBC8 && SEMC8 < SEMC13)
            {
                if (SEMC2 < MAC2 && SEMC3 < UBC3)
                {
                    return CreateTrigger(context, 1.9499M, 1.9499M, basicTrigger, StartRule_1_9499);
                }
                else if (SEMC3 > UBC3)
                {
                    return CreateTrigger(context, 1.9449M, -1.9449M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9419M, 0M, basicTrigger);
                }
            }
            else if (LB2C3 < MAC5)
            {
                if (LB2C2 > MAC5)
                {
                    if (C2_UBLB2_Dir == -1)
                    {
                        if (LBC2 > MAC3)
                        {
                            return CreateTrigger(context, 1.9498M, 1.9498M, basicTrigger);
                        }
                        else if (SEMC3 < UBC3 && SEMC2 < MAC2 && SEMC2 > LBC2)
                        {
                            return CreateTrigger(context, 1.9497M, 1.9497M, basicTrigger);
                        }
                        else if (SEMC3 > UBC3)
                        {
                            return CreateTrigger(context, 1.9448M, -1.9448M, basicTrigger, StartRule_1_9448);
                        }
                        else
                        {
                            return CreateTrigger(context, 1.9418M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC3 > UBC3)
                    {
                        return CreateTrigger(context, 1.9447M, -1.9447M, basicTrigger, StartRule_1_9447);
                    }
                    else if (SEMC2 < MAC2 && SEMC3 > MAC3)
                    {
                        if (LBC2 < MAC3 && C2_UBLB2_Dir == 1)
                        {
                            if (UBC1 < MAC2)
                            {
                                return CreateTrigger(context, 1.9496M, 1.9496M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 1.9446M, -1.9446M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 1.9417M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 1.9416M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 1.9445M, -1.9445M, basicTrigger, StartRule_1_9445);
                }
            }
            else if (LB2C3 >= MAC5)
            {
                if (MAC1 < MAC3)
                {
                    return CreateTrigger(context, 1.9444M, -1.9444M, basicTrigger);
                }
                else if (MAL1 < MAC3 && MAC2 < UBC5)
                {
                    return CreateTrigger(context, 1.9443M, -1.9443M, basicTrigger);
                }
                else if (MAL2 < MAC3 && MAC2 < UBC5)
                {
                    return CreateTrigger(context, 1.9442M, -1.9442M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9441M, -1.9441M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.9M)
        {
            return CreateTrigger(context, 6.9M, -6.9M, basicTrigger);
        }

        if (basicTrigger == 7.99M)
        {
            if (MAL2 < MAC5 && SEMC2 > MAC2 && MAL2 > MAC2 && SEMC1 > MAC1 && MAL2 > MAC1 && MAC1 > SEMC5 && SEMC1 > SEMC5 && MAC1 > SEMC4 && SEMC1 > SEMC4 && SEMC1 > SEMC3 && MAC2 > SEMC5 && SEMC2 > SEMC5 && MAC2 > SEMC4 && SEMC2 > SEMC4 && cParam.C5_Odd_Stage != -1 && C5_SEMTrend != -0.1M)
            {
                return CreateTrigger(context, 7.9989M, 7.9989M, basicTrigger);
            }
            else if (MAL2 > MAC5 && SEMC1 > MAC1 && MAL2 > MAC1 && MAC1 > SEMC5 && SEMC1 > SEMC5 && MAC1 > SEMC4 && SEMC1 > SEMC4 && SEMC1 > SEMC3 && SEMC2 > SEMC3 && SEMC2 > SEMC4 && cParam.C5_Odd_Stage != -1 && C5_SEMTrend != -0.1M)
            {
                return CreateTrigger(context, 7.9988M, 7.9988M, basicTrigger);
            }
            else if (SEMC5 < MAC5)
            {
                if (MAL2 < LB2C5)
                {
                    if (C5_SEMShortTrend != 0 && MAL2 > SEMC1 && MAL2 > SEMC2 && MAL3 > LB2C3 && MAL3 > LBC1 && MAL2 > LB2C8 && SEMC8 > MAC8 && SEMC1 < LB2C5 && SEMC1 < LBC1 && SEMC1 < SEMC2 && SEMC2 < LB2C5 && SEMC2 < LBC1 && SEMC2 < SEMC3 && SEMC3 < LBC3 && SEMC3 < SEMC4 && SEMC3 < SEMC5)
                    {
                        return CreateTrigger(context, 7.9999M, 7.9999M, basicTrigger);
                    }
                    else if (C5_SEMShortTrend == 0 && MAL2 < LB2C13 && MAC1 < MAC13 && SEMC3 < LBC3 && SEMC3 < MAC13 && SEMC5 < MAC13)
                    {
                        return CreateTrigger(context, 7.9998M, 0M, basicTrigger);
                    }
                    else if (C5_SEMShortTrend == 0 && MAL2 < LBC13 && MAL3 > LBC1 && MAL3 > LB2C3 && SEMC3 < MAC3 && SEML1 < MAL2 && MAL2 < LB2C8 && MAL2 < LB2C4 && MAC1 < MAC13 && SEMC3 < LBC3 && SEMC3 < MAC13 && SEMC5 < MAC13)
                    {
                        return CreateTrigger(context, 7.9997M, 0M, basicTrigger);
                    }
                    else if (SEMC1 < MAC1 && SEMC1 < MAC2 && SEMC1 < MAC3 && MAC1 < MAC3 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC4) && MAL2 < MAC1 && SEMC3 < MAC3)
                    {
                        return CreateTrigger(context, 7.9949M, -7.9949M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.9919M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 < LB2C5 || MAL3 < LB2C5)
                {
                    if (cParam.C5_Odd_Stage == 1 && MAL2 < LBC2 && MAL2 > SEMC1 && MAL2 < MAC1 && MAL3 < MAL2 && MAL2 < SEMC3 && SEMC1 < LB2C5 && SEMC1 < LBC1 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC3 < MAC3 && LB2C3 < LB2C5 && SEMC3 < SEMC5 && SEMC5 < MAC5 && SEMC8 > MAC8)
                    {
                        return CreateTrigger(context, 7.9996M, 7.9996M, basicTrigger, StartRule_7_9996);
                    }
                    else if (MAL2 < LBC5 && SEMC1 < MAC2 && SEMC1 < MAC3 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC1 && SEMC1 < MAC1 && SEMC8 < MAC8 && ((SEMC2 > LBC2 && MAL2 < SEMC2) || (SEMC2 < MAC2 && MAL2 < LBC2 && MAL2 < SEMC3 && SEMC3 > LBC3)) && SEMC5 < SEMC8)
                    {
                        return CreateTrigger(context, 7.9948M, -7.9948M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.9918M, 0M, basicTrigger);
                    }
                }
                else if (C5_SEMShortTrend != 0)
                {
                    if (SEMC5 < MAC1 && SEMC1 < SEMC5 && SEMC1 < MAC1 && SEMC4 < MAC1 && SEMC3 < MAC1 && SEMC2 < MAC1 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAC1 < MAC2 && MAC1 < MAC3 && MAC1 < MAC5 && SEMC2 < LBC2 && MAC2 < MAC3 && MAC2 < MAC5 && SEMC2 < SEMC5 && SEMC3 < LBC3 && MAC3 < MAC5 && SEMC3 < SEMC5 && SEMC4 < MAC4 && SEMC2 < SEMC4 && SEMC3 < SEMC4 && SEMC4 < SEMC5)
                    {
                        return CreateTrigger(context, 7.9995M, 7.9995M, basicTrigger);
                    }
                    else if (SEMC5 < MAC1 && SEMC1 > SEMC5 && SEMC1 > MAC1 && SEMC4 < MAC1 && SEMC3 < MAC1 && MAL2 > MAC1 && MAL2 > MAC2 && MAL2 > SEMC5 && MAL2 > SEMC3 && MAC1 > MAC2 && SEMC1 > SEMC2 && SEMC1 > SEMC3 && SEMC1 > SEMC4 && SEMC2 > SEMC3 && SEMC2 > SEMC5 && SEMC2 > MAC2)
                    {
                        return CreateTrigger(context, 7.9994M, 7.9994M, basicTrigger);
                    }
                    else if (SEMC4 < MAC1 && SEMC3 < MAC1 && SEMC2 < MAC1 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && SEMC1 < SEMC5 && SEMC1 < MAC1 && MAC1 < MAC2 && MAC1 < MAC3 && MAC1 < MAC5 && SEMC2 < MAC2 && MAC2 < MAC3 && MAC2 < MAC5 && SEMC2 < SEMC5 && SEMC3 < MAC3 && MAC3 < MAC5 && SEMC3 < SEMC5 && SEMC4 < MAC4 && SEMC2 < SEMC4 && SEMC3 < SEMC4 && SEMC5 > SEMC4 && SEMC5 > MAC1)
                    {
                        return CreateTrigger(context, 7.9993M, 7.9993M, basicTrigger);
                    }
                    else if (SEMC1 < MAC1 && SEMC1 < MAC2 && SEMC1 < MAC3 && MAC1 < MAC3 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC4) && MAL2 < MAC1 && SEMC3 < MAC3)
                    {
                        return CreateTrigger(context, 7.9947M, -7.9947M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.9917M, 0M, basicTrigger);
                    }
                }
                else if (cParam.C5_Odd_Stage == -1 && MAL2 > MAC5 && MAL2 > MAC1 && MAC1 > MAC5 && SEMC1 > MAC1 && SEMC1 > MAL2 && SEMC1 < SEMC8 && MAC2 < MAC5 && SEMC2 > MAC2 && SEMC2 < SEMC8 && MAC3 < MAC5 && SEMC3 > MAC3 && SEMC3 < SEMC8 && SEMC5 < MAC5 && SEMC5 < SEMC8)
                {
                    return CreateTrigger(context, 7.9945M, -7.9945M, basicTrigger, StartRule_7_9945);
                }
                else if (MAL2 < MAC1 && SEMC1 < MAC1 && ((SEMC2 > LBC2 && MAL2 < SEMC2) || (SEMC2 < MAC2 && MAL2 < LBC2 && MAL2 < SEMC3 && SEMC3 > LBC3)))
                {
                    return CreateTrigger(context, 7.9944M, -7.9944M, basicTrigger);
                }
                else if (SEMC1 < MAC1 && SEMC1 < MAC2 && SEMC1 < MAC3 && MAC1 < MAC3 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC4) && MAL2 < MAC1 && SEMC3 < MAC3)
                {
                    return CreateTrigger(context, 7.9946M, -7.9946M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9916M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 >= MAC5)
            {
                if (MAL2 > MAC1 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > MAC5 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 7.9992M, 7.9992M, basicTrigger, additionalStopCondition: StopRule_7_9992);
                }
                else if (SEMC5 < UBC5 && C5_SEMTrend != -0.1M && SEMC1 > SEMC5 && SEMC1 > MAC1 && SEMC4 < MAC1 && SEMC3 < MAC1 && MAL2 > MAC1 && MAL2 > MAC2 && MAL2 > SEMC5 && MAL2 > SEMC3 && MAC1 > MAC2 && SEMC1 > SEMC2 && SEMC1 > SEMC3 && SEMC1 > SEMC4 && SEMC2 > SEMC3 && SEMC2 > SEMC5 && SEMC2 > MAC2)
                {
                    return CreateTrigger(context, 7.9991M, 7.9991M, basicTrigger);
                }
                else if (MAL2 < MAC5)
                {
                    return CreateTrigger(context, 7.9987M, 0M, basicTrigger);
                }
                else if (MAL2 > MAC5 && MAL2 < LBC2 && MAL2 < MAC1 && C5_SEMTrend == -0.1M && (MAC1 < SEMC5 || SEMC5 < UBC5))
                {
                    return CreateTrigger(context, 7.9943M, -7.9943M, basicTrigger);
                }
                else if (MAL2 > MAC5 && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < MAC1 && C5_SEMTrend == -0.1M && (MAC1 < SEMC5 || SEMC5 < UBC5))
                {
                    return CreateTrigger(context, 7.9942M, -7.9942M, basicTrigger, StartRule_7_9942);
                }
                else if (MAL2 > MAC5 && (SEMC2 > MAC2 || SEMC3 > MAC3) && C5_SEMTrend == -0.1M && (MAC1 < SEMC5 || SEMC5 < UBC5))
                {
                    return CreateTrigger(context, 7.9941M, -7.9941M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9915M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.98M)
        {
            if (MAC2 > LBC13 && MAC1 > LB2C13 && MAL1 > LB2C21 && SEMC3 < LBC2)
            {
                if (MAL1 > LB2C5)
                {
                    return CreateTrigger(context, 7.9899M, 7.9899M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9849M, -7.9849M, basicTrigger);
                }
            }
            else if (MAC2 > LBC13 && MAC1 > LB2C13 && MAL1 > LB2C21 && SEMC2 < LBC1)
            {
                return CreateTrigger(context, 7.9898M, 7.9898M, basicTrigger);
            }
            else if ((MAC2 < LBC13 || MAC1 < LB2C13 || MAL1 < LB2C21) && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3 && MAL3 > LB2C3)
            {
                return CreateTrigger(context, 7.9848M, -7.9848M, basicTrigger);
            }
            else if (MAL2 < LB2C4 && SEMC2 < LBC2 && MAL1 > LB2C5 && MAL3 < LB2C3)
            {
                return CreateTrigger(context, 7.9897M, 7.9897M, basicTrigger);
            }
            else if (SEMC2 > LBC1 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && MAL2 < SEMC3)
            {
                return CreateTrigger(context, 7.9847M, -7.9847M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9819M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.97M)
        {
            return CreateTrigger(context, 7.9749M, -7.9749M, basicTrigger);
        }

        if (basicTrigger == 7.96M)
        {
            return CreateTrigger(context, 7.9649M, -7.9649M, basicTrigger);
        }

        if (basicTrigger == 7.95M)
        {
            if (MAC21 < MAC89 && SEMC13 < MAC13 && SEMC8 < MAC8)
            {
                return CreateTrigger(context, 7.9541M, -7.9541M, basicTrigger);
            }
            else if (SEMC5 > LB2C13 && SEMC5 > LB2C21)
            {
                if (C3_SEMShortTrend == 0 && MAC2 < MAC5 && MAC2 < MAC3)
                {
                    return CreateTrigger(context, 7.9549M, -7.9549M, basicTrigger);
                }
                else if (SEMC8 < MAC8)
                {
                    if (UB2C2 < MAC5)
                    {
                        if (MAC3 < LBC8 && UBC2 < MAC3 && MAC2 > LBC13 && MAL2 > LBC13)
                        {
                            return CreateTrigger(context, 7.9599M, 7.9599M, basicTrigger, StartRule_7_9599);
                        }
                        else if (SEMC5 < LB2C13 && SEMC5 < LB2C21 && SEMC5 > LB2C8)
                        {
                            return CreateTrigger(context, 7.9519M, 0M, basicTrigger);
                        }
                        else if (MAC2 < MAC3)
                        {
                            return CreateTrigger(context, 7.9548M, -7.9548M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.9519M, 0M, basicTrigger);
                        }
                    }
                    else if (UB2C2 >= MAC5 && UBC2 < MAC5)
                    {
                        if (C2_MAC_Pivot_Trend == -1 && C5_MAC_Pivot_Trend == 1)
                        {
                            if (MAC2 < LBC13)
                            {
                                return CreateTrigger(context, 7.9547M, -7.9547M, basicTrigger);
                            }
                            else if (MAL3 < MAL2)
                            {
                                return CreateTrigger(context, 7.9598M, 7.9598M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.9519M, 0M, basicTrigger);
                            }
                        }
                        else if (C2_MAC_Pivot_Trend == -1 && C5_MAC_Pivot_Trend == -1)
                        {
                            if (C2_Pivot_Status_Long > 0)
                            {
                                if (MAC3 < LBC13)
                                {
                                    return CreateTrigger(context, 7.9597M, 7.9597M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 7.9546M, -7.9546M, basicTrigger);
                                }
                            }
                            else
                            {
                                return CreateTrigger(context, 7.9545M, -7.9545M, basicTrigger);
                            }
                        }
                        else if (C2_MAC_Pivot_Trend == 1 && C5_MAC_Pivot_Trend == 1)
                        {
                            return CreateTrigger(context, 7.9596M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.9595M, 0M, basicTrigger);
                        }
                    }
                }
                else if (SEMC8 >= MAC8)
                {
                    if (C8_SEM_MA_ConfBreak == -1)
                    {
                        return CreateTrigger(context, 7.9544M, -7.9544M, basicTrigger);
                    }
                    else if (MAL2 > MAC8)
                    {
                        return CreateTrigger(context, 7.9543M, -7.9543M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.9594M, 7.9594M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.9519M, 0M, basicTrigger);
                }
            }
            else if ((SEMC5 < LB2C13 || SEMC5 < LB2C21) && (MAC1 < MAC3 || MAC2 < MAC3) && MAC2 < MAC3)
            {
                return CreateTrigger(context, 7.9542M, -7.9542M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9518M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.94M)
        {
            if (MAL2 >= LBC2 && C3_SEM_MA_ConfBreak == 1 && SEMC3 > MAC3)
            {
                return CreateTrigger(context, 7.9499M, 7.9499M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9449M, -7.9449M, basicTrigger);
            }
        }

        if (basicTrigger == 7.93M)
        {
            if (C2_MAC_Pivot_Trend == 1 && C5_MAC_Pivot_Trend == 1)
            {
                if (LB2C8 > LB2C13)
                {
                    return CreateTrigger(context, 7.9399M, 7.9399M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9319M, 0M, basicTrigger);
                }
            }
            else if (C2_MAC_Pivot_Trend == 1 || C5_MAC_Pivot_Trend == 1)
            {
                if (MAL2 > MAL1 || UBC1 < SEMC4)
                {
                    return CreateTrigger(context, 7.9349M, -7.9349M, basicTrigger, StartRule_7_9349);
                }
                else
                {
                    return CreateTrigger(context, 7.9348M, -7.9348M, basicTrigger);
                }
            }
            else if (SEMC5 > LBC5)
            {
                return CreateTrigger(context, 7.9398M, 7.9398M, basicTrigger);
            }
            else if (C3_C5_LLB2_Cont_MAC == 1)
            {
                if (SEMC1 < SEMC5)
                {
                    return CreateTrigger(context, 7.9397M, 7.9397M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9318M, 0M, basicTrigger);
                }
            }
            else if (MAC1 < LB2C8)
            {
                if (UBC1 > SEMC4 && MAL2 < SEMC4)
                {
                    return CreateTrigger(context, 7.9347M, -7.9347M, basicTrigger);
                }
                else if (UBC1 < SEMC4 && MAL2 > SEMC1 && MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 7.9396M, 7.9396M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9317M, 0M, basicTrigger);
                }
            }
            else if (MAC1 > SEMC4 && MAL2 < SEMC4 && MAC1 < LBC5 && SEMC5 < LBC5 && SEMC1 > SEMC2)
            {
                return CreateTrigger(context, 7.9395M, 7.9395M, basicTrigger);
            }
            else if (MAC1 > SEMC4 && MAL2 < SEMC4 && MAC1 < LBC5 && SEMC5 < LBC5 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC3 < SEMC4 && SEMC4 < SEMC5)
            {
                return CreateTrigger(context, 7.9394M, 7.9394M, basicTrigger);
            }
            else if (MAL2 < MAL1)
            {
                return CreateTrigger(context, 7.9346M, -7.9346M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9316M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.92M)
        {
            if (MAL2 > LB2C21 && MAL3 < LB2C21)
            {
                return CreateTrigger(context, 7.9299M, 7.9299M, basicTrigger);
            }
            else if (MAL2 < LB2C21 && MAL3 < LB2C21)
            {
                return CreateTrigger(context, 7.9249M, -7.9249M, basicTrigger);
            }
            else if (SEMC5 < LBC1 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 7.9248M, -7.9248M, basicTrigger);
            }
            else if (MAL2 < SEMC5 && MAC1 > LBC5)
            {
                return CreateTrigger(context, 7.9298M, 7.9298M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.91M)
        {
            if (MAL2 < SEMC21)
            {
                if (SEMC1 > UBC1 && MAL2 > UBC5)
                {
                    return CreateTrigger(context, 7.9147M, -7.9147M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9118M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > SEMC2 && SEMC1 > SEMC2)
            {
                return CreateTrigger(context, 7.9199M, 7.9199M, basicTrigger);
            }
            else if (MAL3 > UBC1)
            {
                return CreateTrigger(context, 7.9198M, 7.9198M, basicTrigger);
            }
            else if (SEMC1 > SEMC2 && MAC1 > SEMC3)
            {
                return CreateTrigger(context, 7.9197M, 7.9197M, basicTrigger, StartRule_7_9197);
            }
            else if (SEMC1 < SEMC2)
            {
                if (MAL2 < MAL1 && MAC1 > SEMC3 && MAL2 > MAC1 && MAL1 > MAC1 && MAL2 > SEML1)
                {
                    return CreateTrigger(context, 7.9196M, 7.9196M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.9149M, -7.9149M, basicTrigger);
                }
            }
            else if (SEML1 > MAL2 && MAL3 < UBC1 && MAL2 < SEMC2 && MAC1 < SEMC3)
            {
                return CreateTrigger(context, 7.9148M, -7.9148M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.9119M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.89M)
        {
            if (SEMC5 < UBC5 && MAC1 > UBC5)
            {
                if (MAL2 < UBC5 && MAC1 < UBC3)
                {
                    return CreateTrigger(context, 7.8999M, 7.8999M, basicTrigger, StartRule_7_8999);
                }
                else if (SEMC3 < UBC3 && SEMC1 < UBC5 && SEMC2 > SEMC3 && MAL2 > SEMC1)
                {
                    return CreateTrigger(context, 7.8998M, 7.8998M, basicTrigger, StartRule_7_8998);
                }
                else if (MAL2 < SEMC2 && MAL2 < SEMC3 && (MAL2 > UBC5 || MAC1 > UBC3))
                {
                    return CreateTrigger(context, 7.8949M, -7.8949M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8919M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5 && MAC1 < UBC5)
            {
                if (MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < MAC1 && MAL2 > MAC2)
                {
                    return CreateTrigger(context, 7.8948M, -7.8948M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8918M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5 && MAC1 > UBC5)
            {
                if (SEMC2 > SEMC3)
                {
                    if (SEMC5 > UBC3 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.8997M, 7.8997M, basicTrigger);
                    }
                    else if (LBC1 > UBC3 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.8996M, 7.8996M, basicTrigger, StartRule_7_8996);
                    }
                    else if (SEMC3 > SEMC4 && SEMC4 < SEMC5 && MAL2 > UBC3)
                    {
                        return CreateTrigger(context, 7.8995M, 7.8995M, basicTrigger, StartRule_7_8995);
                    }
                    else if (SEMC1 > SEMC5 && SEML2 > UBC5 && MAL2 < SEMC2 && MAL2 < SEMC3 && SEMC5 < UBC3 && LBC1 < UBC3)
                    {
                        return CreateTrigger(context, 7.8947M, -7.8947M, basicTrigger);
                    }
                    else if ((SEMC1 < SEMC5 || SEML2 < UBC5) && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.8994M, 7.8994M, basicTrigger, StartRule_7_8994);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.8917M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 <= SEMC3)
                {
                    if (SEMC5 > UBC3 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.8993M, 7.8993M, basicTrigger);
                    }
                    else if (LBC1 > UBC3 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.8992M, 7.8992M, basicTrigger, StartRule_7_8992);
                    }
                    else if (cParam.C5UB2_C5UB1 == -1)
                    {
                        if (MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < MAC1 && MAL2 > MAC2)
                        {
                            return CreateTrigger(context, 7.8946M, -7.8946M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.8916M, 0M, basicTrigger);
                        }
                    }
                    else if (cParam.C5UB2_C5UB1 == 0)
                    {
                        if (SEMC3 > SEMC4 && SEMC4 < SEMC5 && MAL2 > UBC3)
                        {
                            return CreateTrigger(context, 7.8991M, 7.8991M, basicTrigger);
                        }
                        else if (MAL2 > SEMC1 && MAL3 < SEMC1 && MAL2 > SEMC5 && SEMC2 > UBC2 && SEMC3 > SEMC4 && SEMC3 > SEMC5 && MAL2 < MAC1 && MAL2 < MAL1)
                        {
                            return CreateTrigger(context, 7.8945M, -7.8945M, basicTrigger);
                        }
                        else if (MAL2 > SEMC1 && MAL3 > SEMC1 && MAL2 > SEMC5 && SEMC2 > UBC2 && SEMC3 > SEMC4 && SEMC3 > SEMC5)
                        {
                            return CreateTrigger(context, 7.8989M, 7.8989M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.8915M, 0M, basicTrigger);
                        }
                    }
                }
            }
            else
            {
                return CreateTrigger(context, 7.8914M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.88M)
        {
            if (cParam.L2_C1_C5_Pattern == -1)
            {
                return CreateTrigger(context, 7.8849M, -7.8849M, basicTrigger);
            }
            else if (SEMC5 < UBC5 && cParam.L2_C1_C5_Pattern != -1 && MAC1 > UBC5)
            {
                return CreateTrigger(context, 7.8899M, 7.8899M, basicTrigger);
            }
            else if (SEMC5 < UBC5 && MAC1 < UBC5)
            {
                return CreateTrigger(context, 7.8848M, -7.8848M, basicTrigger);
            }
            else if (SEMC2 < SEMC3 && SEMC2 < SEMC4)
            {
                return CreateTrigger(context, 7.8847M, -7.8847M, basicTrigger);
            }
            else if (MAL2 > UBC5 && MAL2 > SEMC1 && cParam.L2_C1_C5_Pattern == 0 && MAC1 > UBC5 && SEMC2 > SEMC3 && SEMC2 > SEMC4 && SEMC3 > SEMC4 && SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 7.8898M, 7.8898M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.8819M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.86M)
        {
            if (MAC1 >= MAC2)
            {
                return CreateTrigger(context, 7.8699M, 7.8699M, basicTrigger);
            }

            if (MAC1 < MAC2)
            {
                return CreateTrigger(context, 7.8649M, -7.8649M, basicTrigger);
            }
        }

        if (basicTrigger == 7.84M)
        {
            if (SEMC1 < MAC1 && SEMC2 > SEMC3 && cParam.C1LB1_reset == 0)
            {
                return CreateTrigger(context, 7.8499M, 7.8499M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.8449M, -7.8449M, basicTrigger);
            }
        }

        if (basicTrigger == 7.83M)
        {
            if (LB2C1 > MAC2)
            {
                if ((SEMC2 < SEMC8 && MAL2 < SEMC8) || (SEMC1 > SEMC5 && SEMC1 > UBC1) || (SEMC1 < MAC1 && SEMC2 > UB2C1))
                {
                    return CreateTrigger(context, 7.8349M, -7.8349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8319M, 0M, basicTrigger);
                }
            }
            else if (MAC5 > UBC13 && MAC8 > UBC21 && SEMC5 > MAC5 && SEMC8 > UBC8 && SEMC13 > UBC13 && SEMC21 > UBC21)
            {
                if ((SEMC5 < SEMC8 && SEMC2 > UBC1) || (SEMC1 < SEMC2 && SEMC2 < SEMC8 && SEMC3 > SEMC8) || (SEMC1 < MAC1 && SEMC2 > UB2C1))
                {
                    return CreateTrigger(context, 7.8348M, -7.8348M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8318M, 0M, basicTrigger);
                }
            }
            else if (MAL1 > UBC2 && MAL1 > UBC3)
            {
                return CreateTrigger(context, 7.8347M, -7.8347M, basicTrigger);
            }
            else if (MAL2 > UBC2 && MAL2 > UBC3)
            {
                return CreateTrigger(context, 7.8346M, -7.8346M, basicTrigger);
            }
            else if (MAL2 < SEMC13 && MAL2 > MAC3)
            {
                if (MAL2 < MAC2)
                {
                    return CreateTrigger(context, 7.8345M, -7.8345M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8317M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= MAC2)
            {
                if (MAC1 > MAC2 && MAC1 > UBC5 && (MAL2 > MAC1 || MAL1 > MAC1))
                {
                    return CreateTrigger(context, 7.8399M, 7.8399M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8316M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC2)
            {
                if (MAC1 >= MAC2)
                {
                    if ((LBC2 < UBC5 && SEMC2 > UBC3) || (MAL2 < SEMC13))
                    {
                        return CreateTrigger(context, 7.8344M, -7.8344M, basicTrigger);
                    }
                    else if (MAL2 < UBC5)
                    {
                        return CreateTrigger(context, 7.8343M, -7.8343M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.8315M, 0M, basicTrigger);
                    }
                }
                else if (MAC1 < MAC2)
                {
                    if (MAL2 > MAC3 && MAL2 < UBC5 && SEMC2 > LBC2)
                    {
                        return CreateTrigger(context, 7.8342M, -7.8342M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.8314M, 0M, basicTrigger);
                    }
                }
            }
            else
            {
                return CreateTrigger(context, 7.8313M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.77M)
        {
            if (C5_SEMTrend != -0.1M)
            {
                return CreateTrigger(context, 7.7799M, 7.7799M, basicTrigger);
            }
            else if (C5_SEMTrend == -0.1M)
            {
                if (SEMC8 > SEMC21 && SEMC5 > SEMC21 && MAC1 > UBC5 && MAL2 > UBC5 && SEMC5 > UBC5 && SEMC8 > UBC8)
                {
                    return CreateTrigger(context, 7.7798M, 7.7798M, basicTrigger, StartRule_7_7798);
                }
                else if (SEMC8 < SEMC21 && SEMC5 < SEMC21)
                {
                    return CreateTrigger(context, 7.7749M, -7.7749M, basicTrigger);
                }
                else if (MAC1 < UBC5 && MAL2 < UBC5)
                {
                    return CreateTrigger(context, 7.7748M, -7.7748M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7719M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.76M)
        {
            if (SEMC8 < UBC8 && (C8_SEMTrend != -0.1M || SEMC8 < MAC8) && SEMC5 > SEMC8)
            {
                if (SEMC8 <= MAC8)
                {
                    if (SEMC5 <= MAC5)
                    {
                        if (MAL2 > SEMC5 && SEMC1 < UBC1)
                        {
                            return CreateTrigger(context, 7.7693M, 7.7693M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7605M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 > MAC5)
                    {
                        if (MAL2 >= MAC5 && MAL2 >= SEMC5)
                        {
                            return CreateTrigger(context, 7.7692M, 7.7692M, basicTrigger, StartRule_7_7692);
                        }
                        else if (MAL2 < MAC5)
                        {
                            if (SEMC1 > SEMC2 || SEMC4 < MAC4 || MAL2 > SEMC3)
                            {
                                return CreateTrigger(context, 7.7691M, 7.7691M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.7604M, 0M, basicTrigger);
                            }
                        }
                    }
                }
                else if (SEMC8 > MAC8)
                {
                    if (MAL2 >= UBC5)
                    {
                        if (SEMC5 > UBC5 && MAL2 > SEMC8 && SEMC8 < UBC8)
                        {
                            return CreateTrigger(context, 7.7689M, 7.7689M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7603M, 0M, basicTrigger);
                        }
                    }

                    if (MAL2 < UBC5)
                    {
                        if (MAL2 < SEMC5)
                        {
                            return CreateTrigger(context, 7.7644M, -7.7644M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7602M, 0M, basicTrigger);
                        }
                    }
                }
            }
            else if (MAL2 > SEMC21)
            {
                if (SEMC3 > SEMC5 && MAL2 > SEMC3 && MAL2 > SEMC5 && SEMC5 < MAC5)
                {
                    return CreateTrigger(context, 7.7699M, 7.7699M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7619M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5)
            {
                if (SEMC3 > MAC3 && SEMC5 > SEMC8 && SEMC5 > SEMC13 && MAC1 > MAC3 && MAL2 > MAC5 && MAC1 > MAC5)
                {
                    return CreateTrigger(context, 7.7698M, 7.7698M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7618M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 > UBC13 && SEMC8 < UBC8)
            {
                if (SEMC8 > MAC8)
                {
                    if (SEMC3 > SEMC5 && MAL2 > SEMC3 && MAL2 > SEMC5 && SEMC5 < MAC5 && SEMC3 < MAC3)
                    {
                        return CreateTrigger(context, 7.7697M, 7.7697M, basicTrigger);
                    }
                    else if (SEMC3 <= SEMC5 && (MAL2 < SEMC3 || MAL2 < SEMC2 || MAL2 < SEMC1 || MAL2 < SEMC5))
                    {
                        return CreateTrigger(context, 7.7649M, -7.7649M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7617M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 <= MAC8 && SEMC8 > LBC8)
                {
                    if (SEMC5 > LBC5)
                    {
                        return CreateTrigger(context, 7.7648M, -7.7648M, basicTrigger);
                    }
                    else if (SEMC3 < SEMC5 && SEMC2 < SEMC3)
                    {
                        return CreateTrigger(context, 7.7647M, -7.7647M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7616M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 <= LBC8)
                {
                    return CreateTrigger(context, 7.7615M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 > UBC8 && SEMC13 < UBC13)
            {
                if (SEMC5 > UBC5 && SEMC3 > MAC3 && SEMC5 > SEMC8 && SEMC8 > SEMC13 && MAC1 > MAC3)
                {
                    return CreateTrigger(context, 7.7696M, 7.7696M, basicTrigger);
                }
                else if (SEMC8 > UBC8 && SEMC3 < MAC3 && SEMC4 < UBC8 && SEMC5 < UBC5 && SEMC5 < SEMC8 && SEMC13 < UBC8 && SEMC21 < UBC8)
                {
                    return CreateTrigger(context, 7.7646M, -7.7646M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7614M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 < UBC13 && SEMC8 < UBC8)
            {
                if (SEMC8 < MAC8 && SEMC8 > LBC8)
                {
                    if (SEMC5 < LBC5)
                    {
                        if (SEMC3 < MAC3 && (MAL2 > SEMC3 || MAL2 > SEMC5 || SEMC2 > SEMC3 || SEMC3 > SEMC5))
                        {
                            return CreateTrigger(context, 7.7695M, 7.7695M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7613M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC3 > MAC3 && SEMC5 < SEMC8 && MAL2 < SEMC3 && MAL2 < SEMC2 && MAL2 < SEMC1 && SEMC1 > UBC1)
                    {
                        return CreateTrigger(context, 7.7612M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7611M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 >= MAC8 && SEMC5 < UBC5)
                {
                    if (SEMC5 < MAC5 && SEMC8 > MAC5 && SEMC3 < SEMC5)
                    {
                        return CreateTrigger(context, 7.7645M, -7.7645M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7609M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 < LBC8 && SEMC5 < LBC5)
                {
                    if (MAL2 > SEMC5 && MAL2 > SEMC8 && MAL2 > SEMC3 && SEMC2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.7694M, 7.7694M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7608M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.7607M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.7606M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.74M)
        {
            if (SEMC13 > MAC13 && SEMC8 > LBC8)
            {
                if (SEMC5 < LBC5)
                {
                    if (SEMC4 < LBC4 && SEMC3 < LBC3 && SEMC2 < LBC2)
                    {
                        return CreateTrigger(context, 7.7499M, 7.7499M, basicTrigger, StartRule_7_7499);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7419M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 >= LBC5)
                {
                    if (MAL1 > LB2C8 || MAL1 > LB2C13)
                    {
                        return CreateTrigger(context, 7.7498M, 7.7498M, basicTrigger);
                    }
                    else if (MAL1 < LB2C8 && MAL1 < LB2C13)
                    {
                        return CreateTrigger(context, 7.7449M, -7.7449M, basicTrigger, additionalStopCondition: StopRule_Negative_7_7449);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7418M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC13 < MAC13 && SEMC8 < LBC8)
            {
                return CreateTrigger(context, 7.7497M, 7.7497M, basicTrigger);
            }
            else if ((SEMC13 < MAC13 && SEMC8 > LBC8) || (SEMC13 > MAC13 && SEMC8 < LBC8))
            {
                if (MAL1 < LB2C8 && MAL1 < LB2C13)
                {
                    return CreateTrigger(context, 7.7448M, -7.7448M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7417M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.69M)
        {
            if (MAC13 > MAC21)
            {
                return CreateTrigger(context, 7.6949M, -7.6949M, basicTrigger);
            }
            else if (MAL2 < UB2C5 && MAL2 > MAC21 && SEMC2 > SEMC3 && MAL2 > SEMC4)
            {
                if (SEMC1 > SEMC3)
                {
                    return CreateTrigger(context, 7.6999M, 7.6999M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6919M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > UB2C5)
            {
                return CreateTrigger(context, 7.6948M, -7.6948M, basicTrigger);
            }
            else if (MAL2 < MAC21)
            {
                return CreateTrigger(context, 7.6947M, -7.6947M, basicTrigger);
            }
            else if (MAL2 < SEMC4)
            {
                return CreateTrigger(context, 7.6946M, -7.6946M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6918M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.65M)
        {
            if (MAL2 < MAC21)
            {
                if (C13_UBLB2_Dir == 1)
                {
                    if (SEMC2 > SEMC5 && SEMC3 > SEMC5)
                    {
                        if (SEMC2 < MAC2)
                        {
                            return CreateTrigger(context, 7.6599M, 7.6599M, basicTrigger, StartRule_7_6599);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6519M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 > SEMC8 && (SEMC2 > SEMC8 || SEMC3 > SEMC8 || SEMC2 > SEMC5 || SEMC3 > SEMC5) && (MAL2 > SEMC5 || MAL2 > SEMC8))
                    {
                        if (SEMC2 < MAC2)
                        {
                            return CreateTrigger(context, 7.6584M, 7.6584M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6506M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < SEMC5 && MAL2 < LBC5 && MAL3 > LB2C8 && MAL3 > LB2C13 && MAL2 > LB2C8 && MAL2 > LB2C13 && MAL1 > LB2C8 && MAL1 > LB2C13 && SEMC5 < SEMC8)
                    {
                        return CreateTrigger(context, 7.6549M, -7.6549M, basicTrigger);
                    }
                    else if (MAL2 < SEMC5 && MAL2 < LBC5 && SEMC5 < SEMC8 && SEMC4 < SEMC5 && SEMC3 < SEMC4 && SEMC2 < SEMC3 && (MAL2 > LB2C8 || MAL2 > LB2C13) && (MAL1 > LB2C8 || MAL1 > LB2C13))
                    {
                        return CreateTrigger(context, 7.6545M, -7.6545M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6518M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.6517M, 0M, basicTrigger);
                }
            }
            else if (C13_UBLB2_Dir == 1)
            {
                if (MAL2 < LBC5)
                {
                    if (SEMC2 > SEMC5 || SEMC3 > SEMC5)
                    {
                        return CreateTrigger(context, 7.6598M, 7.6598M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6597M, 7.6597M, basicTrigger, StartRule_7_6597);
                    }
                }
                else if (MAL2 < LBC8 || MAL2 < LBC13)
                {
                    if (SEMC2 > SEMC5 && SEMC3 > SEMC5 && (SEMC2 > SEMC8 || SEMC3 > SEMC8) && (MAL2 > MAC3 || MAL2 > MAC2 || MAL2 > MAC1) && MAL2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.6596M, 7.6596M, basicTrigger);
                    }
                    else if (SEMC5 > MAC5 && SEMC5 < UBC5 && (MAL2 > SEMC1 || MAL2 > SEMC2))
                    {
                        return CreateTrigger(context, 7.6595M, 7.6595M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6516M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 >= LBC5)
                {
                    if (MAL2 > MAC5 && MAL2 > MAC8 && MAL2 > MAC13)
                    {
                        if (MAL2 > UBC5)
                        {
                            if (SEMC5 > MAC5 && MAL2 > SEMC5 && MAL2 > MAC1 && SEMC1 > MAC1 && MAL1 > MAC1 && MAL2 < UB2C13 && MAL2 < UB2C8)
                            {
                                return CreateTrigger(context, 7.6594M, 7.6594M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.6515M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 <= UBC5)
                        {
                            if (MAL2 > MAC5 && MAL2 > MAC8 && MAL2 > SEMC5)
                            {
                                return CreateTrigger(context, 7.6593M, 7.6593M, basicTrigger, StartRule_7_6593);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.6514M, 0M, basicTrigger);
                            }
                        }
                    }
                    else if (MAL2 > MAC5 && MAL2 > MAC8)
                    {
                        if (MAL2 > SEMC5)
                        {
                            return CreateTrigger(context, 7.6592M, 7.6592M, basicTrigger, StartRule_7_6592);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6513M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 > MAC5)
                    {
                        if (SEMC5 > MAC5 && MAL2 > SEMC5)
                        {
                            return CreateTrigger(context, 7.6591M, 7.6591M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6512M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 <= MAC5)
                    {
                        if (SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && MAL2 > SEMC5 && MAL2 > SEMC3 && SEMC2 > SEMC3)
                        {
                            return CreateTrigger(context, 7.6589M, 7.6589M, basicTrigger);
                        }
                        else if (SEMC5 > MAC5 && SEMC5 < UBC5 && (MAL2 > SEMC3 || MAL2 > SEMC2))
                        {
                            return CreateTrigger(context, 7.6588M, 7.6588M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6511M, 0M, basicTrigger);
                        }
                    }
                }
            }
            else if (C13_UBLB1_Dir == 1)
            {
                if (SEMC5 < MAC13 && SEMC8 < MAC13 && MAC1 < MAC13 && SEMC5 > SEMC8 && (MAL2 > SEMC8 || MAL2 > SEMC5))
                {
                    return CreateTrigger(context, 7.6587M, 7.6587M, basicTrigger);
                }
                else if (MAL2 > MAC13)
                {
                    return CreateTrigger(context, 7.6548M, -7.6548M, basicTrigger, StartRule_7_6548);
                }
                else if (MAL2 < MAC13)
                {
                    return CreateTrigger(context, 7.6547M, -7.6547M, basicTrigger, StartRule_7_6547);
                }
                else
                {
                    return CreateTrigger(context, 7.6509M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5)
            {
                if ((MAL2 > MAC5 || MAL2 > MAC8) && (SEMC5 > SEMC13 || SEMC5 > SEMC8) && (MAL2 > SEMC13 || MAL2 > SEMC5))
                {
                    return CreateTrigger(context, 7.6586M, 7.6586M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6508M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5 && MAL2 > SEMC5 && MAL2 < UB2C13 && MAL2 < UB2C8 && SEMC1 > MAC1)
            {
                return CreateTrigger(context, 7.6585M, 7.6585M, basicTrigger);
            }
            else if (SEMC5 > UBC5 && C5_SEMTrend == -0.1M && MAL2 < SEMC5 && MAL2 < SEMC3)
            {
                return CreateTrigger(context, 7.6546M, -7.6546M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6507M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.63M)
        {
            if (MAL2 > UBC5)
            {
                if (SEMC5 > UBC5)
                {
                    if (MAL2 < SEMC5 && MAL2 < SEMC3 && MAL2 < SEMC2 && MAL2 < MAC1 && MAL2 > UBC5)
                    {
                        return CreateTrigger(context, 7.6349M, -7.6349M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6319M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 >= MAC5)
                {
                    if (SEMC1 >= MAC1)
                    {
                        if (MAL2 > MAC1 && MAL1 > MAC1 && SEMC2 > SEMC3 && SEMC2 >= MAC2)
                        {
                            return CreateTrigger(context, 7.6399M, 7.6399M, basicTrigger, StartRule_7_6399);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6318M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 7.6348M, -7.6348M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6317M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 < MAC5 && MAL2 < SEMC1 && MAL2 < SEMC2 && (SEMC1 > UBC1 || SEMC2 > UBC1))
                {
                    return CreateTrigger(context, 7.6347M, -7.6347M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6316M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > MAC5)
            {
                if (SEMC5 >= MAC5)
                {
                    return CreateTrigger(context, 7.6398M, 7.6398M, basicTrigger);
                }
                else if (SEMC5 < MAC5)
                {
                    if (SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 7.6397M, 7.6397M, basicTrigger);
                    }
                    else if (MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.6346M, -7.6346M, basicTrigger);
                    }
                    else if (SEML1 > UBC1 && SEML2 > UBC1 && SEMC1 > UBC1)
                    {
                        return CreateTrigger(context, 7.6345M, -7.6345M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6315M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.6314M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < LBC5)
            {
                if (SEMC5 >= LBC5)
                {
                    return CreateTrigger(context, 7.6396M, 7.6396M, basicTrigger);
                }
                else if (SEMC5 < LBC5)
                {
                    if (MAC21 > MAC34 && MAC21 > MAC89)
                    {
                        if (MAL2 > SEMC13 || SEMC5 > SEMC13)
                        {
                            return CreateTrigger(context, 7.6395M, 7.6395M, basicTrigger, StartRule_7_6395);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6313M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC21 < MAC34 && MAC21 < MAC89)
                    {
                        return CreateTrigger(context, 7.6394M, 7.6394M, basicTrigger);
                    }
                    else if ((MAC21 < MAC34 && MAC21 > MAC89) || (MAC21 > MAC34 && MAC21 < MAC89))
                    {
                        if (SEMC21 <= LBC13)
                        {
                            return CreateTrigger(context, 7.6393M, 7.6393M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6344M, -7.6344M, basicTrigger);
                        }
                    }
                }
            }
            else if (MAL2 <= MAC5 && MAL2 >= LBC5)
            {
                if (SEMC5 < SEMC21)
                {
                    if (SEMC1 < LBC5 || MAL1 < LBC5)
                    {
                        if (MAL2 > SEMC5 && MAL2 > SEMC1)
                        {
                            return CreateTrigger(context, 7.6392M, 7.6392M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.6312M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6343M, -7.6343M, basicTrigger);
                    }
                }

                if (SEMC5 >= SEMC21)
                {
                    if (MAL2 > SEMC21 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.6391M, 7.6391M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.6342M, -7.6342M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 7.62M)
        {
            if (MAL2 < SEMC13 && MAL2 < LBC5 && MAL2 < MAC1)
            {
                return CreateTrigger(context, 7.6249M, -7.6249M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.61M)
        {
            if (C21_UBLB2_Dir == 1)
            {
                if (MAL2 < MAC3)
                {
                    return CreateTrigger(context, 7.6149M, -7.6149M, basicTrigger);
                }
                else if (MAL2 >= MAC3)
                {
                    return CreateTrigger(context, 7.6148M, -7.6148M, basicTrigger, StartRule_7_6148);
                }
            }
            else if (SEMC5 > SEMC13)
            {
                return CreateTrigger(context, 7.6199M, 7.6199M, basicTrigger, StartRule_7_6199);
            }
            else if (MAL2 < MAC3)
            {
                return CreateTrigger(context, 7.6147M, -7.6147M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6119M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.59M)
        {
            return CreateTrigger(context, 7.5949M, -7.5949M, basicTrigger);
        }

        if (basicTrigger == 7.58M)
        {
            return CreateTrigger(context, 7.5849M, -7.5849M, basicTrigger);
        }

        if (basicTrigger == 7.57M)
        {
            if (MAC1 < LB2C13 && MAC1 < LB2C8)
            {
                return CreateTrigger(context, 7.5719M, 0M, basicTrigger);
            }
            else if (MAC1 < LB2C13 && MAC1 > LB2C8)
            {
                return CreateTrigger(context, 7.5719M, 0M, basicTrigger);
            }
            else if (MAC1 > LB2C13 && MAC1 < LB2C8)
            {
                return CreateTrigger(context, 7.5719M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.55M)
        {
            if (C21_UBLB2_Dir == -1 && C21_UBLB1_Dir == 1)
            {
                return CreateTrigger(context, 7.5549M, -7.5549M, basicTrigger);
            }
            else if (SEMC13 < LBC13)
            {
                if (SEMC5 < SEMC13 && SEMC3 > SEMC5 && SEMC2 > SEMC5 && MAC2 < LBC8 && SEMC3 < MAC3 && SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 7.5599M, 7.5599M, basicTrigger, StartRule_7_5599);
                }
                else
                {
                    return CreateTrigger(context, 7.5519M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < SEMC13 && MAC2 < LBC8 && SEMC3 < MAC3 && SEMC2 < MAC2)
            {
                return CreateTrigger(context, 7.5598M, 7.5598M, basicTrigger, StartRule_7_5598);
            }
            else if (MAC2 > LBC8 && MAL2 < MAL1 && MAL2 < SEMC3 && MAL2 < SEMC5)
            {
                return CreateTrigger(context, 7.5548M, -7.5548M, basicTrigger);
            }
            else if (SEMC2 > MAC2 && SEMC1 < MAC1 && MAL2 < MAL1 && MAL2 < SEMC2)
            {
                return CreateTrigger(context, 7.5547M, -7.5547M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.5518M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.54M)
        {
            if (MAC21 < MAC89)
            {
                if (SEMC5 < LB2C21 && SEMC5 < LB2C13 && (MAC1 > MAC2 || MAC1 > MAC3))
                {
                    return CreateTrigger(context, 7.5419M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5449M, -7.5449M, basicTrigger);
                }
            }
            else if (SEMC21 > LBC21)
            {
                if (SEMC5 < LB2C21 && SEMC5 < LB2C13 && (MAC1 > MAC2 || MAC1 > MAC3))
                {
                    return CreateTrigger(context, 7.5418M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5448M, -7.5448M, basicTrigger);
                }
            }
            else if (SEMC21 <= LBC21)
            {
                if (SEMC5 > SEMC13 || SEMC5 > SEMC21)
                {
                    return CreateTrigger(context, 7.5447M, -7.5447M, basicTrigger);
                }
                else if (MAC2 > LBC8 && C2_C8_MAS_LB1C_Cont_MAC == 0)
                {
                    return CreateTrigger(context, 7.5446M, -7.5446M, basicTrigger);
                }
                else if (SEMC5 < LB2C21 && SEMC5 < LB2C13)
                {
                    if (MAC1 > MAC2 || MAC1 > MAC3)
                    {
                        return CreateTrigger(context, 7.5499M, 7.5499M, basicTrigger, StartRule_7_5499);
                    }
                    else if (SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > SEMC2)
                    {
                        return CreateTrigger(context, 7.5498M, 7.5498M, basicTrigger, StartRule_7_5498);
                    }
                    else if (MAC1 < MAC2 && MAC1 < MAC3 && (MAL2 < SEMC1 || MAL2 < SEMC2) && MAL2 < MAC3)
                    {
                        return CreateTrigger(context, 7.5445M, -7.5445M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.5417M, 0M, basicTrigger);
                    }
                }
                else if (SEMC13 < LBC13)
                {
                    if (MAL2 < LB2C21 || MAL2 < LB2C13)
                    {
                        if (SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > SEMC2 && (SEML2 < MAL2 || SEML1 < MAL2 || MAL3 < MAL2))
                        {
                            return CreateTrigger(context, 7.5497M, 7.5497M, basicTrigger, StartRule_7_5497);
                        }
                        else if (SEMC1 > MAC1 || MAL2 < SEMC1 || MAL2 < SEMC2)
                        {
                            return CreateTrigger(context, 7.5444M, -7.5444M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.5416M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC13 < SEMC21 && (MAL2 < SEMC13 || MAL2 < SEMC21) && MAL2 < SEMC8)
                    {
                        return CreateTrigger(context, 7.5443M, -7.5443M, basicTrigger);
                    }
                    else if (SEMC13 < SEMC21 && (MAL2 < SEMC13 || MAL2 < SEMC21) && MAL2 < SEMC8)
                    {
                        return CreateTrigger(context, 7.5415M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.5414M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.53M)
        {
            if (SEMC8 < LBC8)
            {
                if (MAC1 < LB2C21)
                {
                    return CreateTrigger(context, 7.5319M, 0M, basicTrigger);
                }
                else if (SEMC8 < LB2C13 && SEMC5 < LB2C13)
                {
                    return CreateTrigger(context, 7.5318M, 0M, basicTrigger);
                }
                else if (SEMC5 > SEMC8)
                {
                    return CreateTrigger(context, 7.5317M, 0M, basicTrigger);
                }
                else if (MAC1 < LB2C13 && SEMC2 < SEMC8 && SEMC3 < SEMC8 && SEMC3 < SEMC5)
                {
                    return CreateTrigger(context, 7.5349M, -7.5349M, basicTrigger);
                }
                else if (MAC1 > LB2C13 && SEMC2 < SEMC8 && SEMC3 < SEMC8)
                {
                    return CreateTrigger(context, 7.5348M, -7.5348M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && SEMC1 < SEMC2)
                {
                    return CreateTrigger(context, 7.5347M, -7.5347M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5316M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 > LBC8 && SEMC8 < MAC8)
            {
                if (SEMC8 > SEMC13)
                {
                    if (MAL2 < MAC8 && MAC1 < MAC8)
                    {
                        if (MAL2 < LBC8 && MAL2 < LBC5)
                        {
                            return CreateTrigger(context, 7.5399M, 7.5399M, basicTrigger, StartRule_7_5399);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.5315M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 > UBC5)
                    {
                        if (SEMC1 < UBC1 && MAL2 > SEMC5 && MAL2 > MAC13)
                        {
                            return CreateTrigger(context, 7.5398M, 7.5398M, basicTrigger);
                        }
                        else if (MAL2 < UBC5 && SEMC1 < MAC1 && SEMC1 > SEMC5 && MAL2 > MAC13)
                        {
                            return CreateTrigger(context, 7.5397M, 7.5397M, basicTrigger);
                        }
                        else if (SEMC1 > UBC1 && MAL2 < SEMC1)
                        {
                            return CreateTrigger(context, 7.5346M, -7.5346M, basicTrigger);
                        }
                        else if (SEMC1 < UBC1 && SEMC1 > MAC1 && MAL2 < SEMC1 && MAL2 < SEMC5 && MAL2 < MAC13 && MAL2 < SEMC21)
                        {
                            return CreateTrigger(context, 7.5345M, -7.5345M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.5314M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 > MAC5 && SEMC5 < UBC5)
                    {
                        if (MAL2 > UBC5 && MAL2 < SEMC21)
                        {
                            return CreateTrigger(context, 7.5344M, -7.5344M, basicTrigger);
                        }
                        else if (MAL2 < UBC5 && SEMC1 < MAC1 && SEMC1 > SEMC5)
                        {
                            return CreateTrigger(context, 7.5396M, 7.5396M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.5313M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 7.5312M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 >= SEMC8)
                {
                    if (SEMC5 > UBC5)
                    {
                        if (SEMC1 < UBC1)
                        {
                            return CreateTrigger(context, 7.5395M, 7.5395M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.5343M, -7.5343M, basicTrigger);
                        }
                    }
                    else if (SEMC1 > UBC1)
                    {
                        return CreateTrigger(context, 7.5342M, -7.5342M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.5311M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 < SEMC8)
                {
                    return CreateTrigger(context, 7.5341M, -7.5341M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5309M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 > MAC8)
            {
                if (SEMC8 > SEMC13)
                {
                    if ((MAL2 < LBC5 || MAL2 < SEMC13) && SEMC5 > MAC5)
                    {
                        return CreateTrigger(context, 7.5394M, 7.5394M, basicTrigger);
                    }
                    else if (MAL2 > UBC5 && MAL2 > SEMC8 && SEMC5 > UBC5 && MAL2 < SEMC13)
                    {
                        return CreateTrigger(context, 7.5393M, 7.5393M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.5308M, 0M, basicTrigger);
                    }
                }
            }
            else
            {
                return CreateTrigger(context, 7.5307M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.52M)
        {
            if (MAC21 < MAC89 && MAC13 > MAC89 && SEMC21 < MAC13)
            {
                if (SEMC5 < LB2C13 && MAC1 < LB2C13)
                {
                    return CreateTrigger(context, 7.5298M, 7.5298M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5219M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 < LB2C13)
            {
                if (SEMC5 < LB2C13)
                {
                    return CreateTrigger(context, 7.5299M, 7.5299M, basicTrigger);
                }
                else if (MAL2 < SEMC13)
                {
                    return CreateTrigger(context, 7.5249M, -7.5249M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5218M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 < LB2C13 && SEMC5 < LB2C21)
            {
                if (MAL2 < SEMC13)
                {
                    return CreateTrigger(context, 7.5248M, -7.5248M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.5217M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.5247M, -7.5247M, basicTrigger);
            }
        }

        if (basicTrigger == 7.51M)
        {
            if ((SEMC2 > SEMC5 && SEMC3 > SEMC5) && (SEMC2 > SEMC8 || SEMC3 > SEMC8))
            {
                return CreateTrigger(context, 7.5199M, 7.5199M, basicTrigger);
            }
            else if (MAL2 > MAC1 && MAL1 > MAC1 && (SEMC2 > SEMC5 || SEMC3 > SEMC5 || SEMC1 > SEMC8))
            {
                return CreateTrigger(context, 7.5198M, 7.5198M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.5119M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.49M)
        {
            if (MAL2 < SEMC5 && MAL2 < SEMC8)
            {
                return CreateTrigger(context, 7.4945M, -7.4945M, basicTrigger);
            }
            else if (MAL2 < LBC5 || MAL2 < LBC8)
            {
                if (SEMC1 < MAC1 || C1_SEMTrend != -0.1M)
                {
                    return CreateTrigger(context, 7.4999M, 7.4999M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4919M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > UBC5 || MAL2 > UBC8)
            {
                if (SEMC5 > MAC5)
                {
                    if (SEMC1 <= UBC1)
                    {
                        return CreateTrigger(context, 7.4998M, 7.4998M, basicTrigger);
                    }

                    if (SEMC1 > UBC1)
                    {
                        return CreateTrigger(context, 7.4949M, -7.4949M, basicTrigger);
                    }
                }
                else if (SEMC1 > UBC1 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 7.4948M, -7.4948M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4918M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 > SEMC5 || SEMC3 > SEMC5)
            {
                if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 7.4997M, 7.4997M, basicTrigger, StartRule_7_4997);
                }
                else if (SEMC1 > UBC1 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 7.4947M, -7.4947M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4917M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 > UBC1 && MAL2 < SEMC1)
            {
                return CreateTrigger(context, 7.4946M, -7.4946M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.4916M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.48M)
        {
            if (MAL2 > MAC1 && MAL2 > MAL1 && MAL1 > MAC1 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > SEMC5 && SEMC8 > SEMC13)
            {
                return CreateTrigger(context, 7.4899M, 7.4899M, basicTrigger);
            }
            else if (SEMC8 < SEMC13 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC5))
            {
                return CreateTrigger(context, 7.4849M, -7.4849M, basicTrigger);
            }
            else if (MAL2 < MAC1 && MAL1 < MAC1 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC5))
            {
                return CreateTrigger(context, 7.4848M, -7.4848M, basicTrigger);
            }
            else if (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC5)
            {
                return CreateTrigger(context, 7.4847M, -7.4847M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.4819M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.47M)
        {
            if (MAL2 >= SEMC21)
            {
                if (MAL2 < MAC21)
                {
                    if (MAL2 > MAC5 && MAL2 > MAC8 && MAL2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.4799M, 7.4799M, basicTrigger);
                    }
                    else if (MAL2 > MAC5 && MAL2 > MAC8 && MAL2 <= SEMC5)
                    {
                        if (SEMC1 < MAC1)
                        {
                            return CreateTrigger(context, 7.4798M, 7.4798M, basicTrigger, StartRule_7_4798);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.4719M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < MAC8 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.4749M, -7.4749M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4718M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > MAC21 && MAL2 > MAC13 && MAL2 > UBC5 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 7.4748M, -7.4748M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4717M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < SEMC21 && MAL2 > LBC21)
            {
                if (SEMC2 < LBC21 && MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 7.4797M, 7.4797M, basicTrigger);
                }
                else if (MAL2 > MAC8 && MAL2 > MAC5)
                {
                    if (SEMC3 > SEMC5)
                    {
                        return CreateTrigger(context, 7.4796M, 7.4796M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4716M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > LBC21 && MAL1 > LBC21 && MAC1 > LBC21 && SEMC2 > LBC21 && SEMC3 > LBC21)
                {
                    return CreateTrigger(context, 7.4747M, -7.4747M, basicTrigger);
                }
                else if (MAL2 > LBC21 && MAL1 > LBC21 && MAL2 > MAC5 && MAL2 < MAC8 && SEMC2 > LBC21 && SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 7.4746M, -7.4746M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4715M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < SEMC21 && MAL2 <= LBC21)
            {
                if (SEMC5 <= MAC5)
                {
                    if (MAL2 <= MAC5)
                    {
                        if (SEMC5 <= LBC5)
                        {
                            if (MAL2 > SEMC3 && MAL2 > SEMC5)
                            {
                                return CreateTrigger(context, 7.4795M, 7.4795M, basicTrigger);
                            }
                            else if (MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC5)
                            {
                                return CreateTrigger(context, 7.4745M, -7.4745M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.4714M, 0M, basicTrigger);
                            }
                        }

                        if (SEMC5 > LBC5)
                        {
                            if (SEMC3 > SEMC5)
                            {
                                return CreateTrigger(context, 7.4794M, 7.4794M, basicTrigger);
                            }
                            else if (MAL2 > MAC1 && MAL2 > MAL1)
                            {
                                if (SEML2 < UBC1 && SEML1 < UBC1)
                                {
                                    return CreateTrigger(context, 7.4793M, 7.4793M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 7.4713M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAC1 && MAL1 < MAC1 && SEMC3 < SEMC5 && SEML2 < MAC5)
                            {
                                return CreateTrigger(context, 7.4744M, -7.4744M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.4712M, 0M, basicTrigger);
                            }
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4743M, -7.4743M, basicTrigger);
                    }
                }
                else if (SEMC5 > MAC5)
                {
                    if (SEMC3 > SEMC5)
                    {
                        return CreateTrigger(context, 7.4792M, 7.4792M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4742M, -7.4742M, basicTrigger);
                    }
                }
            }
            else
            {
                return CreateTrigger(context, 7.4711M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.46M)
        {
            if (SEMC1 > MAC1)
            {
                return CreateTrigger(context, 7.4649M, -7.4649M, basicTrigger);
            }
            else if (MAL2 > MAC21 || MAL1 > MAC21 || MAC1 > MAC21)
            {
                if (MAC1 > MAC21)
                {
                    return CreateTrigger(context, 7.4648M, -7.4648M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4619M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < SEMC13)
            {
                if (SEMC8 >= SEMC21)
                {
                    if (SEMC1 < SEMC13 && SEMC2 < SEMC13 && SEMC3 < SEMC13)
                    {
                        return CreateTrigger(context, 7.4699M, 7.4699M, basicTrigger, StartRule_7_4699);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4647M, -7.4647M, basicTrigger);
                    }
                }
                else if (SEMC8 < SEMC21)
                {
                    if (MAL2 < SEMC5 && MAL2 < SEMC8 && (MAL2 < SEMC2 || MAL2 < SEMC3))
                    {
                        return CreateTrigger(context, 7.4646M, -7.4646M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4618M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC8 < SEMC13 && SEMC8 > SEMC21 && SEMC5 > MAC5 && MAL2 > MAC8 && MAL2 > MAC5)
            {
                return CreateTrigger(context, 7.4698M, 7.4698M, basicTrigger, StartRule_7_4698);
            }
            else if (SEMC8 > UBC8 && SEMC5 > UBC5 && (MAC1 > UBC8 || MAC1 > UBC5 || MAC1 > MAC3))
            {
                return CreateTrigger(context, 7.4697M, 7.4697M, basicTrigger);
            }
            else if (SEMC5 > MAC5 && SEMC8 < UBC8 && SEMC5 < UBC5 && C5_SEMTrend != -0.1M && C8_SEMTrend != -0.1M && MAL2 > SEMC5 && MAL2 > SEMC8)
            {
                return CreateTrigger(context, 7.4696M, 7.4696M, basicTrigger);
            }
            else if (MAL2 < MAC1 && MAL1 < MAC1 && ((MAL2 < SEMC3 && MAL2 < SEMC5) || (MAL2 < SEMC2 && MAL2 < SEMC3)))
            {
                return CreateTrigger(context, 7.4645M, -7.4645M, basicTrigger);
            }
            else if (MAL2 < MAC3 && SEMC5 > MAC5 && SEMC3 < SEMC5)
            {
                return CreateTrigger(context, 7.4644M, -7.4644M, basicTrigger);
            }
            else if ((MAL2 < MAC1 || MAL1 < MAC1) && ((MAL2 < SEMC3 && MAL2 < SEMC5) || (MAL2 < SEMC2 && MAL2 < SEMC3)))
            {
                return CreateTrigger(context, 7.4643M, -7.4643M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.4617M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.45M)
        {
            if (SEMC13 < LBC13)
            {
                return CreateTrigger(context, 7.4549M, -7.4549M, basicTrigger);
            }
            else if (SEMC13 >= MAC13)
            {
                if (MAL2 >= MAC1)
                {
                    if (SEMC1 > SEMC2 || MAL2 > SEMC2)
                    {
                        return CreateTrigger(context, 7.4599M, 7.4599M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4519M, 0M, basicTrigger);
                    }
                }

                if (MAL2 < MAC1)
                {
                    if (SEMC2 < MAC2 && SEMC3 < MAC3 && (SEMC2 < SEMC13 || SEMC3 < SEMC13))
                    {
                        return CreateTrigger(context, 7.4598M, 7.4598M, basicTrigger);
                    }
                    else if (SEMC2 > MAC2)
                    {
                        return CreateTrigger(context, 7.4548M, -7.4548M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.4518M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAL2 > MAC21 && MAL2 < MAC5 && SEMC5 > MAC5 && MAL2 < SEMC3 && MAL2 < SEMC5)
            {
                return CreateTrigger(context, 7.4547M, -7.4547M, basicTrigger, StartRule_7_4547);
            }
            else if (MAL2 > MAC21 && MAL2 > MAC5 && MAL2 < UBC5 && SEMC5 > UBC5 && SEMC3 < SEMC5 && MAL2 < SEMC3 && MAL2 < SEMC5)
            {
                return CreateTrigger(context, 7.4546M, -7.4546M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.4545M, -7.4545M, basicTrigger, StartRule_7_4545);
            }
        }

        if (basicTrigger == 7.44M)
        {
            if (MAL2 < SEMC21 && MAL2 <= LB2C21)
            {
                if ((SEMC1 < MAC1 || C1_SEMTrend != -0.1M) && MAL2 > MAC1)
                {
                    return CreateTrigger(context, 7.4499M, 7.4499M, basicTrigger);
                }
                else if (MAL2 < MAC1 && MAL2 < SEMC4 && MAL2 < SEMC5)
                {
                    if (MAL2 <= MAL1)
                    {
                        return CreateTrigger(context, 7.4449M, -7.4449M, basicTrigger);
                    }
                    else if (MAL2 > MAL1)
                    {
                        return CreateTrigger(context, 7.4448M, -7.4448M, basicTrigger, StartRule_7_4448);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.4419M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.4418M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.43M)
        {
            if (SEMC13 < LBC8 || (MAL2 < LB2C13 && MAL2 < LB2C21))
            {
                return CreateTrigger(context, 7.4349M, -7.4349M, basicTrigger);
            }
            else if (MAL2 > SEMC1 && SEMC1 < MAC1 && MAL2 > LB2C13 && MAL2 > LB2C21 && SEMC13 > LBC8 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 7.4399M, 7.4399M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.4319M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.42M)
        {
            if (MAL2 > MAC1 && MAL1 > MAC1 && SEML2 > MAC1 && SEML1 > MAC1 && SEMC1 > MAC1)
            {
                return CreateTrigger(context, 7.4299M, 7.4299M, basicTrigger);
            }
            else if (SEMC5 > SEMC8 && SEMC5 > SEMC13 && SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 7.4298M, 7.4298M, basicTrigger);
            }
            else if (MAC21 < MAC89)
            {
                return CreateTrigger(context, 7.4249M, -7.4249M, basicTrigger);
            }
            else if (SEMC5 < LB2C21 && SEMC5 < LB2C13 && SEMC3 < LB2C21 && SEMC3 < LB2C13 && SEMC2 < LB2C21 && SEMC2 < LB2C13 && MAL2 < LB2C21 && MAL2 < LB2C13)
            {
                return CreateTrigger(context, 7.4297M, 7.4297M, basicTrigger);
            }
            else if (SEMC5 < LB2C21)
            {
                return CreateTrigger(context, 7.4248M, -7.4248M, basicTrigger, StartRule_7_4248);
            }
            else
            {
                return CreateTrigger(context, 7.4247M, -7.4247M, basicTrigger, StartRule_7_4247);
            }
        }

        if (basicTrigger == 7.41M)
        { // done

            // && SEMC21 < LBC21 
            // && SEMC89 > MAC89 && MAC21 > MAC89
            // && MAL2 <= MAC89 && MAC8 < MAC89) {   

            if (SEMC13 > MAC5 || SEMC13 > MAC8 || SEMC13 > MAC13)
            {
                if (MAL2 < SEMC13)
                {
                    return CreateTrigger(context, 7.4149M, -7.4149M, basicTrigger);
                }
                else if (MAL2 >= SEMC13)
                {
                    return CreateTrigger(context, 7.4199M, 7.4199M, basicTrigger);
                }
            }
            else if (SEMC21 <= LBC13)
            {
                if (SEMC3 < MAC3 && MAC1 > LBC89
                    && (SEMC5 < MAC5 || SEMC8 < MAC8))
                {
                    return CreateTrigger(context, 7.4198M, 7.4198M, basicTrigger);
                }
                else if (SEMC3 > UBC3 && MAL2 < UBC5
                    && SEMC2 < SEMC3
                    && MAL2 < SEMC3 && MAL2 < SEMC5)
                {
                    return CreateTrigger(context, 7.4148M, -7.4148M, basicTrigger);
                }
                else if (SEMC5 > MAC5 && SEMC8 > MAC8
                && SEMC3 < MAC3
                && MAL2 < SEMC5 && MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 7.4147M, -7.4147M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4119M, 0, basicTrigger);
                }
            }
            else if (SEMC21 > LBC13)
            {
                if (SEMC13 < LB2C21 && SEMC2 > MAC2 && SEMC3 > MAC3
                    && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC21)
                {
                    return CreateTrigger(context, 7.4146M, -7.4146M, basicTrigger);
                }
                else if (MAL2 < LBC5 && SEMC5 > LBC5
                    && (SEMC3 > LBC5 || SEMC4 > LBC5))
                {
                    return CreateTrigger(context, 7.4145M, -7.4145M, basicTrigger);
                }
                else if (MAL2 < LBC89)
                {
                    return CreateTrigger(context, 7.4144M, -7.4144M, basicTrigger);
                }
                else if (SEMC3 < MAC3 && SEMC2 < MAC2
                    && SEMC8 > LBC8 && SEMC13 < LBC13 && SEMC5 > LBC8
                    && (SEMC5 > MAC5 || SEMC8 > MAC8))
                {
                    return CreateTrigger(context, 7.4197M, 7.4197M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.4118M, 0, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.36M)
        {
            if (SEMC8 < UBC5 && SEMC8 > UBC8)
            {
                if (C8_UBLB2_Dir == 1)
                {
                    return CreateTrigger(context, 7.3699M, 7.3699M, basicTrigger);
                }
                else if (MAL2 < SEMC4 && MAL2 < SEMC5 && MAL2 < SEMC8 && MAC1 < UBC8 && MAC1 < UBC5 && SEMC4 > MAC3 && SEMC4 > MAC4)
                {
                    return CreateTrigger(context, 7.3649M, -7.3649M, basicTrigger);
                }
                else if (MAL2 < SEMC5 && MAL2 < SEMC8 && MAC1 < UBC8 && MAC1 < UBC5 && (SEMC4 < MAC3 || SEMC4 < MAC4))
                {
                    return CreateTrigger(context, 7.3648M, -7.3648M, basicTrigger, StartRule_7_3648);
                }
                else
                {
                    return CreateTrigger(context, 7.3619M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 < UBC5 && SEMC8 < UBC8)
            {
                if (SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC3 < SEMC4)
                {
                    return CreateTrigger(context, 7.3647M, -7.3647M, basicTrigger);
                }
                else if (C5_SEMTrend != -0.1M)
                {
                    return CreateTrigger(context, 7.3618M, 0M, basicTrigger);
                }
                else if (C5_UBLB2_Dir == 1)
                {
                    if (MAL2 < MAC3 && MAL2 > MAC5 && (MAL2 > SEMC1 || MAL2 > SEMC2))
                    {
                        return CreateTrigger(context, 7.3698M, 7.3698M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3617M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 < MAC3 && (SEMC2 < MAC2 || C2_SEMTrend != -0.1M) && MAL2 > LBC3 && (MAL2 > SEMC3 || MAL2 > SEMC2))
                {
                    return CreateTrigger(context, 7.3697M, 7.3697M, basicTrigger);
                }
                else if (SEMC3 < MAC3 && SEMC2 < MAC2 && MAL2 < LBC3 && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 7.3646M, -7.3646M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.3616M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.3615M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.35M)
        {
            if (SEMC8 >= UBC8)
            {
                if (MAC2 > UBC8)
                {
                    if (SEMC5 > Math.Max(UB2C13, UB2C21) && SEMC5 > SEMC8)
                    {
                        return CreateTrigger(context, 7.3549M, -7.3549M, basicTrigger);
                    }
                    else if (SEMC5 > SEMC8)
                    {
                        if (MAC1 > MAC5 && MAC2 > MAC3)
                        {
                            return CreateTrigger(context, 7.3599M, 7.3599M, basicTrigger, StartRule_7_3599);
                        }
                        else if (MAC2 < MAC3 && MAL2 < SEMC2)
                        {
                            return CreateTrigger(context, 7.3548M, -7.3548M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.3519M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 > LBC2)
                    {
                        return CreateTrigger(context, 7.3598M, 7.3598M, basicTrigger, StartRule_7_3598, additionalStopCondition: StopRule_7_3598);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3518M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < MAC5 && MAC2 < UBC8)
                {
                    return CreateTrigger(context, 7.3547M, -7.3547M, basicTrigger);
                }
                else if (SEMC5 >= SEMC8)
                {
                    if (MAL2 > MAC4 && SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 7.3597M, 7.3597M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3517M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 < SEMC8)
                {
                    if (MAL2 > UBC13 && SEMC13 > UBC13 && C13_SEMTrend != -0.1M)
                    {
                        return CreateTrigger(context, 7.3596M, 7.3596M, basicTrigger);
                    }
                    else if ((SEMC4 < MAC4 || SEMC4 < MAC3) && MAL2 < MAC3 && MAL2 > MAC5)
                    {
                        return CreateTrigger(context, 7.3595M, 7.3595M, basicTrigger);
                    }
                    else if (SEMC4 > UBC1)
                    {
                        return CreateTrigger(context, 7.3546M, -7.3546M, basicTrigger);
                    }
                    else if (MAL2 > SEMC13 && SEMC1 > SEMC13 && SEMC2 > SEMC13 && SEMC3 > SEMC13)
                    {
                        if (SEMC1 < MAC1 && SEMC2 > MAC2 && C3_SEMTrend != -0.1M)
                        {
                            return CreateTrigger(context, 7.3594M, 7.3594M, basicTrigger);
                        }
                        else if (MAC1 > MAC3)
                        {
                            return CreateTrigger(context, 7.3593M, 7.3593M, basicTrigger);
                        }
                        else if (MAC1 < MAC3)
                        {
                            return CreateTrigger(context, 7.3545M, -7.3545M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.3516M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < SEMC1 && MAL2 < SEMC5 && SEMC1 > UBC1)
                    {
                        return CreateTrigger(context, 7.3544M, -7.3544M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3515M, 0M, basicTrigger);
                    }
                }
            }
            else
            {
                return CreateTrigger(context, 7.3514M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.34M)
        {
            if (SEMC13 > MAC13 && SEMC21 > LBC21 && SEMC21 < UBC21 && (SEMC21 < SEMC89 || SEMC21 < SEMC55) && MAC21 > MAC89 && SEMC13 > SEMC21 && SEMC89 < UBC21 && SEMC55 < UBC21 && SEMC34 < UBC21 && SEMC89 > MAC21 && SEMC55 > MAC21)
            {
                if (MAC3 <= MAC13)
                {
                    return CreateTrigger(context, 7.3449M, -7.3449M, basicTrigger);
                }
                else if (MAC3 > MAC13)
                {
                    if (SEMC5 < MAC5 && SEMC3 < LBC3 && MAC2 > MAC13)
                    {
                        return CreateTrigger(context, 7.3499M, 7.3499M, basicTrigger);
                    }
                    else if (C3_SEMTrend == -0.1M && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 7.3448M, -7.3448M, basicTrigger);
                    }
                    else if (SEMC5 > MAC5 || SEMC3 > LBC3)
                    {
                        return CreateTrigger(context, 7.3447M, -7.3447M, basicTrigger);
                    }
                    else if (MAC2 < MAC13)
                    {
                        return CreateTrigger(context, 7.3446M, -7.3446M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3419M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAL2 < UBC13)
            {
                if (MAL2 > MAC13 && MAL2 > MAC21 && MAL2 > SEMC13 && MAL2 > SEMC21)
                {
                    if (MAL2 > UBC21)
                    {
                        return CreateTrigger(context, 7.3498M, 7.3498M, basicTrigger);
                    }
                    else if (MAL2 < UBC21 && MAL2 > MAC1 && C1_SEMTrend != -0.1M)
                    {
                        return CreateTrigger(context, 7.3445M, -7.3445M, basicTrigger);
                    }
                    else if (MAL2 < UBC21 && MAL2 < MAC1)
                    {
                        return CreateTrigger(context, 7.3444M, -7.3444M, basicTrigger);
                    }
                    else if (MAL2 < UBC21 && MAL2 < SEMC1 && C1_SEMTrend == -0.1M)
                    {
                        return CreateTrigger(context, 7.3443M, -7.3443M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3418M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 < MAC8)
                {
                    if (SEMC8 < LBC8)
                    {
                        if (SEMC5 < MAC5 && MAL2 > SEMC5)
                        {
                            return CreateTrigger(context, 7.3497M, 7.3497M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.3417M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 < MAC5)
                    {
                        return CreateTrigger(context, 7.3496M, 7.3496M, basicTrigger, StartRule_7_3496);
                    }
                    else if (SEMC5 > MAC5 && C5_SEMTrend != -0.1M)
                    {
                        if (MAL2 > SEMC5)
                        {
                            return CreateTrigger(context, 7.3495M, 7.3495M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.3416M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3415M, 0M, basicTrigger);
                    }
                }
                else if (SEMC8 > UBC8 && SEMC5 < UBC8 && SEMC4 < UBC8 && SEMC3 < UBC8 && SEMC2 < UBC8 && SEMC1 < UBC8)
                {
                    return CreateTrigger(context, 7.3442M, -7.3442M, basicTrigger);
                }
                else if (MAL1 > LBC5 && MAL2 > LBC5 && MAL2 < SEMC5 && MAL1 < SEMC5)
                {
                    return CreateTrigger(context, 7.3441M, -7.3441M, basicTrigger);
                }
                else if (SEMC5 < MAC5)
                {
                    if (MAL2 < MAC5)
                    {
                        return CreateTrigger(context, 7.3494M, 7.3494M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3414M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 < MAC3)
                {
                    if (MAL2 < MAC3)
                    {
                        return CreateTrigger(context, 7.3493M, 7.3493M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3413M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.3412M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= UBC13)
            {
                if (SEMC5 <= UBC5)
                {
                    if (SEMC5 > MAC5 && C5_SEMTrend != -0.1M && MAL2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.3492M, 7.3492M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3411M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 > UBC5)
                {
                    if (SEMC5 < SEMC8)
                    {
                        return CreateTrigger(context, 7.3439M, -7.3439M, basicTrigger, StartRule_7_3439);
                    }
                    else if (SEMC8 < UBC8)
                    {
                        if (SEMC5 > UBC5 && C5_SEMTrend != -0.1M && MAL2 > SEMC5)
                        {
                            return CreateTrigger(context, 7.3487M, 7.3487M, basicTrigger);
                        }
                        else if (MAL2 < SEMC1 && MAL2 < SEMC5)
                        {
                            return CreateTrigger(context, 7.3438M, -7.3438M, basicTrigger);
                        }
                        else if (MAL2 >= SEMC1 && MAL2 < SEMC5)
                        {
                            return CreateTrigger(context, 7.3437M, -7.3437M, basicTrigger, StartRule_7_3437);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.3405M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC8 >= UBC8)
                    {
                        if (MAC1 > UBC5)
                        {
                            if (C8_SEMTrend != -0.1M && SEMC1 > MAC1 && MAL2 > MAC1 && MAL1 > MAC1 && SEMC4 > SEMC5)
                            {
                                return CreateTrigger(context, 7.3491M, 7.3491M, basicTrigger);
                            }
                            else if (SEMC1 >= MAC1)
                            {
                                return CreateTrigger(context, 7.3436M, -7.3436M, basicTrigger, StartRule_7_3436);
                            }
                            else if (SEMC2 > UB2C1)
                            {
                                return CreateTrigger(context, 7.3435M, -7.3435M, basicTrigger, StartRule_7_3435);
                            }
                            else if (MAL2 < MAC2)
                            {
                                return CreateTrigger(context, 7.3434M, -7.3434M, basicTrigger);
                            }
                            else if (MAL2 > UBC4)
                            {
                                return CreateTrigger(context, 7.3433M, -7.3433M, basicTrigger);
                            }
                            else if (SEMC1 < MAC1)
                            {
                                return CreateTrigger(context, 7.3489M, 7.3489M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.3409M, 0M, basicTrigger);
                            }
                        }

                        if (MAC1 <= UBC5)
                        {
                            if (MAL2 > MAC3 && SEMC1 < MAC1 && SEMC2 < UBC2 && MAL2 < MAC1 && MAL1 < MAC1)
                            {
                                return CreateTrigger(context, 7.3488M, 7.3488M, basicTrigger);
                            }
                            else if (MAL2 < MAC3 || SEMC2 > UBC2 || SEMC1 > MAC1)
                            {
                                return CreateTrigger(context, 7.3432M, -7.3432M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.3408M, 0M, basicTrigger);
                            }
                        }
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.3407M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.3406M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.32M)
        {
            if (MAL2 >= SEMC13 && MAL2 >= SEMC21)
            {
                if (SEMC1 <= MAC1)
                {
                    if (MAL2 > MAC2)
                    {
                        return CreateTrigger(context, 7.3299M, 7.3299M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3219M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 > MAC1)
                {
                    if (MAL2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.3218M, 0M, basicTrigger);
                    }

                    if (MAL2 <= SEMC5)
                    {
                        return CreateTrigger(context, 7.3249M, -7.3249M, basicTrigger);
                    }
                }
            }
            else if (MAL2 < SEMC13 && MAL2 < SEMC21)
            {
                return CreateTrigger(context, 7.3298M, 7.3298M, basicTrigger);
            }
            else if (MAL2 < SEMC13 && MAL2 > SEMC21)
            {
                return CreateTrigger(context, 7.3248M, -7.3248M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.3217M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.29M)
        {
            if (C8_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 7.2998M, 7.2998M, basicTrigger);
            }
            else if (MAL1 < LBC3 && MAL3 < LBC3)
            {
                return CreateTrigger(context, 7.2949M, -7.2949M, basicTrigger);
            }
            else if (SEMC3 <= MAC3)
            {
                if (SEMC1 > SEMC2)
                {
                    return CreateTrigger(context, 7.2999M, 7.2999M, basicTrigger, StartRule_7_2999, additionalStopCondition: StopRule_7_2999);
                }
                else
                {
                    return CreateTrigger(context, 7.2919M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 > MAC3)
            {
                return CreateTrigger(context, 7.2948M, -7.2948M, basicTrigger, StartRule_7_2948);
            }
        }

        if (basicTrigger == 7.28M)
        {
            if (SEMC5 < LB2C13 && SEMC5 < LB2C21)
            {
                if (MAL1 < LB2C13 && MAL1 < LB2C21 && MAL2 < LB2C13 && MAL2 < LB2C21)
                {
                    return CreateTrigger(context, 7.2899M, 7.2899M, basicTrigger);
                }
                else if (MAL2 < MAL1 && MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 7.2849M, -7.2849M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2819M, 0M, basicTrigger);
                }
            }
            else if (MAL1 > MAC1 && MAL2 > MAC1 && MAL2 > MAC5 && MAL2 < UBC5)
            {
                if ((MAL2 > SEMC2 && MAL2 > SEMC3) || C5_UBLB1_Dir == 1)
                {
                    return CreateTrigger(context, 7.2898M, 7.2898M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2818M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC1 && MAL2 < SEMC2 && MAL2 > MAC5)
            {
                return CreateTrigger(context, 7.2848M, -7.2848M, basicTrigger);
            }
            else if (MAL2 < SEMC1 && MAL2 < SEMC2 && MAL2 > UBC5 && SEMC1 > UBC1)
            {
                return CreateTrigger(context, 7.2847M, -7.2847M, basicTrigger);
            }
            else if (MAL2 < MAC1 && MAL2 < SEMC5 && MAL2 < LBC5)
            {
                return CreateTrigger(context, 7.2846M, -7.2846M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.2817M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.26M)
        {
            if (MAL1 > SEMC3 && MAL2 > SEMC3 && MAL3 > SEMC3)
            {
                if (MAL2 > UB2C5)
                {
                    return CreateTrigger(context, 7.2699M, 7.2699M, basicTrigger, StartRule_7_2699);
                }
                else
                {
                    return CreateTrigger(context, 7.2619M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.2649M, -7.2649M, basicTrigger);
            }
        }

        if (basicTrigger == 7.25M)
        {
            if (MAL2 < MAC5)
            {
                if (MAL1 > MAC5)
                {
                    if (SEMC8 < UBC5 && SEMC8 < UBC8 && SEMC5 > UBC5)
                    {
                        return CreateTrigger(context, 7.2549M, -7.2549M, basicTrigger);
                    }
                    else if (SEMC5 < SEMC8 && (SEMC3 > UBC5 || SEMC2 > UBC5))
                    {
                        return CreateTrigger(context, 7.2548M, -7.2548M, basicTrigger);
                    }
                    else if (LBC2 > MAC5 && SEMC2 < LBC2)
                    {
                        return CreateTrigger(context, 7.2599M, 7.2599M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2519M, 0M, basicTrigger);
                    }
                }
                else if (MAL1 <= MAC5)
                {
                    return CreateTrigger(context, 7.2547M, -7.2547M, basicTrigger);
                }
            }
            else if (SEMC8 < UBC8)
            {
                if (MAC1 >= MAC3)
                {
                    return CreateTrigger(context, 7.2598M, 7.2598M, basicTrigger);
                }
                else if (MAC1 < MAC3 && MAC1 < SEMC8 && SEMC3 < SEMC5)
                {
                    return CreateTrigger(context, 7.2546M, -7.2546M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2518M, 0M, basicTrigger);
                }
            }
            else if (LB2C2 >= UBC8)
            {
                if (SEMC2 <= LBC2)
                {
                    return CreateTrigger(context, 7.2597M, 7.2597M, basicTrigger);
                }
                else if (SEMC2 > LBC2 && MAL2 < UBC5 && MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 7.2545M, -7.2545M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2517M, 0M, basicTrigger);
                }
            }
            else if (LBC2 >= UBC8)
            {
                if (SEMC3 <= MAC3)
                {
                    return CreateTrigger(context, 7.2516M, 0M, basicTrigger);
                }
                else if (SEMC5 < UB2C8 && SEMC21 > LBC21 && SEMC8 > SEMC13 && MAL3 > LB2C2 && SEMC3 > MAC3 && MAL2 < MAC1)
                {
                    return CreateTrigger(context, 7.2544M, -7.2544M, basicTrigger);
                }
                else if (SEMC3 > MAC3 && MAL2 < MAC1)
                {
                    return CreateTrigger(context, 7.2543M, -7.2543M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2515M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 > UBC3 && MAL2 < MAC1)
            {
                return CreateTrigger(context, 7.2542M, -7.2542M, basicTrigger);
            }
            else if (MAC3 > UBC8)
            {
                if (MAC2 < UBC5 && MAC1 > MAC3 && SEMC2 < MAC3)
                {
                    if (MAC5 > UBC21 && MAC1 > UBC8)
                    {
                        return CreateTrigger(context, 7.2596M, 7.2596M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2514M, 0M, basicTrigger);
                    }
                }
                else if ((MAC1 < MAC3 || SEMC2 > MAC3) && MAL2 < MAC1)
                {
                    return CreateTrigger(context, 7.2541M, -7.2541M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2513M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 < LBC2)
            {
                return CreateTrigger(context, 7.2512M, 0M, basicTrigger);
            }
            else if (MAL1 > LBC2 && SEMC3 < SEMC5 && SEMC3 < SEMC8)
            {
                return CreateTrigger(context, 7.2539M, -7.2539M, basicTrigger);
            }
            else if (MAL1 < LBC2)
            {
                return CreateTrigger(context, 7.2511M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.2509M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.24M)
        {
            return CreateTrigger(context, 7.2499M, 7.2499M, basicTrigger);
        }

        if (basicTrigger == 7.23M)
        {
            if (SEMC3 >= SEMC5)
            {
                if (MAL2 > SEMC5)
                {
                    return CreateTrigger(context, 7.2399M, 7.2399M, basicTrigger);
                }
                else if (SEMC1 <= MAC1)
                {
                    return CreateTrigger(context, 7.2398M, 7.2398M, basicTrigger, StartRule_7_2398);
                }
                else if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 7.2397M, 7.2397M, basicTrigger, StartRule_7_2397);
                }
            }
            else if (SEMC3 < SEMC5)
            {
                if (SEMC2 < SEMC3)
                {
                    return CreateTrigger(context, 7.2349M, -7.2349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2319M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.22M)
        {
            if (SEMC5 > UB2C8)
            {
                if (SEMC5 > UB2C13 && SEMC5 > UB2C21)
                {
                    if (MAL2 < SEMC1 || MAL2 < MAC1)
                    {
                        return CreateTrigger(context, 7.2249M, -7.2249M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2219M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.2299M, 7.2299M, basicTrigger);
                }
            }
            else if (MAL2 > SEMC8)
            {
                if (SEMC1 <= MAC1)
                {
                    return CreateTrigger(context, 7.2298M, 7.2298M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && MAL2 < SEMC1 && (MAL2 < SEMC2 || MAL2 < SEMC3))
                {
                    return CreateTrigger(context, 7.2248M, -7.2248M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2218M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < UBC8)
            {
                if (SEMC4 > UBC5 || SEMC3 > UBC5 || SEMC2 > UBC5)
                {
                    return CreateTrigger(context, 7.2247M, -7.2247M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2217M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= UBC8)
            {
                if (MAL2 >= UBC5)
                {
                    if (SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 7.2297M, 7.2297M, basicTrigger);
                    }
                    else if (SEMC1 > UBC1 && SEMC2 > UBC1)
                    {
                        return CreateTrigger(context, 7.2246M, -7.2246M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2216M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < UBC5)
                {
                    if (SEMC4 < SEMC8 && SEMC3 < SEMC4)
                    {
                        return CreateTrigger(context, 7.2296M, 7.2296M, basicTrigger);
                    }
                    else if (MAL2 < SEMC2 && MAL2 < MAL1)
                    {
                        return CreateTrigger(context, 7.2245M, -7.2245M, basicTrigger);
                    }
                    else if (MAL2 < MAC1)
                    {
                        return CreateTrigger(context, 7.2244M, -7.2244M, basicTrigger, StartRule_7_2244);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2215M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 7.21M)
        {
            if (C5_SEMTrend == -0.1M)
            {
                if (MAL2 < MAC5 && MAL2 < SEMC5 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < MAC1 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC2 < SEMC3 && SEMC3 < SEMC4)
                {
                    return CreateTrigger(context, 7.2149M, -7.2149M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.2119M, 0M, basicTrigger);
                }
            }
            else if (C5_SEMTrend != -0.1M)
            {
                if (MAL2 > MAC5)
                {
                    if (SEMC1 > UBC1 && SEMC2 > UBC1 && SEMC1 < SEMC2 && MAL2 > UBC5 && MAL2 < SEMC1 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.2148M, -7.2148M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2118M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 <= MAC5)
                {
                    if (SEMC2 < MAC2 && MAL2 > LBC5 && MAL2 > SEMC2 && MAL2 > SEMC1)
                    {
                        return CreateTrigger(context, 7.2199M, 7.2199M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.2117M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 7.19M)
        {
            if (MAL2 >= SEMC5)
            {
                return CreateTrigger(context, 7.1999M, 7.1999M, basicTrigger);
            }
            else if (MAL2 < SEMC5)
            {
                if (MAL2 <= LB2C5)
                {
                    return CreateTrigger(context, 7.1998M, 7.1998M, basicTrigger, StartRule_7_1998);
                }
                else if (MAL2 <= LBC5)
                {
                    if (MAL2 >= LBC3)
                    {
                        return CreateTrigger(context, 7.1997M, 7.1997M, basicTrigger);
                    }
                    else if (MAL2 < LBC3)
                    {
                        if (SEMC4 < MAC4 && SEMC4 > LBC4 && MAL2 < SEMC4)
                        {
                            return CreateTrigger(context, 7.1949M, -7.1949M, basicTrigger);
                        }
                        else if (SEMC4 < LBC4 && MAL2 < SEMC4 && UBC1 > MAC2)
                        {
                            return CreateTrigger(context, 7.1947M, -7.1947M, basicTrigger, StartRule_7_1947);
                        }
                        else if (SEMC4 < LBC4 && MAL2 < SEMC4 && UBC1 < MAC5)
                        {
                            return CreateTrigger(context, 7.1948M, -7.1948M, basicTrigger, StartRule_7_1948);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.1919M, 0M, basicTrigger);
                        }
                    }
                }
                else if (MAL2 <= MAC5)
                {
                    return CreateTrigger(context, 7.1918M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1917M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.18M)
        {
            if (MAL2 >= MAC5)
            {
                return CreateTrigger(context, 7.1899M, 7.1899M, basicTrigger);
            }

            if (MAL2 < MAC5)
            {
                if (MAL2 >= LBC3)
                {
                    return CreateTrigger(context, 7.1898M, 7.1898M, basicTrigger);
                }
                else if (MAL2 < LBC3)
                {
                    if (MAL2 < SEMC2 && MAL2 < SEMC3 && SEMC3 < MAC3)
                    {
                        return CreateTrigger(context, 7.1849M, -7.1849M, basicTrigger);
                    }
                    else if (MAL2 < SEMC3 && SEMC3 < MAC3 && SEMC4 < MAC4)
                    {
                        return CreateTrigger(context, 7.1848M, -7.1848M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1819M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 7.17M)
        {
            if (SEMC5 >= MAC5)
            {
                if (MAL2 > SEMC5)
                {
                    if (MAL2 > UBC5)
                    {
                        return CreateTrigger(context, 7.1799M, 7.1799M, basicTrigger);
                    }

                    if (MAL2 > UBC5)
                    {
                        return CreateTrigger(context, 7.1719M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > MAC5)
                {
                    if (SEMC5 < UBC5 && UBC5 > LBC21 && MAL2 < MAC8 && MAL2 < MAC13 && SEMC8 < MAC8 && MAL2 < SEMC5)
                    {
                        return CreateTrigger(context, 7.1749M, -7.1749M, basicTrigger);
                    }
                    else if (SEMC1 > MAC1 && SEMC2 > MAC2 && MAL2 < SEMC1 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.1748M, -7.1748M, basicTrigger);
                    }
                    else if (MAL2 > UBC5 && MAL2 > UBC8)
                    {
                        return CreateTrigger(context, 7.1798M, 7.1798M, basicTrigger);
                    }
                    else if (MAL2 < UBC5 && MAC1 < UBC5 && SEMC5 > UBC5 && SEMC8 > UBC8 && MAL2 < SEMC5 && MAL2 < SEMC8)
                    {
                        return CreateTrigger(context, 7.1747M, -7.1747M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1718M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 <= MAC5)
                {
                    if (SEMC5 < UBC5 && UBC5 > LBC21 && MAL2 < MAC8 && MAL2 < MAC13 && SEMC8 < MAC8 && MAL2 < SEMC5 && SEMC1 < MAC1 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 7.1746M, -7.1746M, basicTrigger);
                    }
                    else if (UBC5 > LBC21 && MAL2 < SEMC8 && MAL2 < SEMC5 && (SEMC5 < SEMC8 || SEMC5 > UBC5 || SEMC8 > UBC8 || (MAL2 < LBC3 && MAL2 < LBC2 && MAL2 < SEMC3)))
                    {
                        return CreateTrigger(context, 7.1745M, -7.1745M, basicTrigger);
                    }
                    else if (MAL2 > MAC1 && MAL1 > MAC1 && SEMC1 > MAC1)
                    {
                        return CreateTrigger(context, 7.1797M, 7.1797M, basicTrigger);
                    }
                    else if (MAL2 > MAC1 && MAL1 > MAC1 && SEMC1 > MAC1)
                    {
                        return CreateTrigger(context, 7.1717M, 0M, basicTrigger);
                    }
                }
            }

            if (SEMC5 < MAC5)
            {
                if (MAL2 > LBC3 && SEMC3 < MAC3 && SEMC2 < UBC2 && SEMC1 < UBC1 && (MAL2 > SEMC8 || MAL2 > SEMC5))
                {
                    return CreateTrigger(context, 7.1796M, 7.1796M, basicTrigger);
                }
                else if (MAL2 < LBC3)
                {
                    if (MAL2 < SEMC3 && SEMC3 > LBC3 && (SEMC2 < LBC2 || SEMC1 < LBC1))
                    {
                        return CreateTrigger(context, 7.1744M, -7.1744M, basicTrigger);
                    }
                    else if (MAL2 < SEMC4 && SEMC4 < LBC4 && SEMC3 < LBC3 && SEMC2 < LBC2)
                    {
                        return CreateTrigger(context, 7.1743M, -7.1743M, basicTrigger);
                    }
                    else if (MAL2 < LBC3 && SEMC8 < LBC3 && SEMC3 > SEMC8 && SEMC2 < LBC2)
                    {
                        return CreateTrigger(context, 7.1742M, -7.1742M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1716M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 <= MAC5)
                {
                    if ((cClose > UBC1 && SEMC1 < UBC1) || (cClose > UBC1 && cClose > SEMC1 && SEMC1 > UBC1))
                    {
                        return CreateTrigger(context, 7.1795M, 7.1795M, basicTrigger, additionalStopCondition: StopRule_7_1795);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1741M, -7.1741M, basicTrigger, StartRule_7_1741);
                    }
                }
                else if (MAL2 > MAC5)
                {
                    if ((MAL2 < UBC5 || MAL2 < MAC8) && MAL2 < UBC8 && MAL2 < SEMC2 && SEMC2 > MAC2)
                    {
                        return CreateTrigger(context, 7.1739M, -7.1739M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1715M, 0M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 7.16M)
        {
            if (SEMC1 <= UBC1)
            {
                return CreateTrigger(context, 7.1699M, 7.1699M, basicTrigger, StartRule_7_1699);
            }
            else if (SEMC1 > UBC1 && C1_SEMTrend == -0.1M)
            {
                return CreateTrigger(context, 7.1649M, -7.1649M, basicTrigger, StartRule_7_1649);
            }
            else
            {
                return CreateTrigger(context, 7.1619M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.15M)
        {
            if (SEMC5 < SEMC8 || MAL2 < SEMC8)
            {
                if (SEMC8 > UBC8)
                {
                    if (SEMC1 < UBC1 && SEML1 < UBC1 && SEML1 < UBC1 && SEMC13 > UBC8)
                    {
                        return CreateTrigger(context, 7.1599M, 7.1599M, basicTrigger, additionalStopCondition: StopRule_7_1599);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1549M, -7.1549M, basicTrigger, StartRule_7_1549);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.1519M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.14M)
        {
            if (SEMC8 < SEMC13)
            {
                if (SEMC13 >= UBC13)
                {
                    return CreateTrigger(context, 7.1499M, 7.1499M, basicTrigger);
                }
                else if (SEMC13 < UBC13)
                {
                    if (MAL2 > SEMC13 && MAL2 > SEMC21)
                    {
                        if (SEMC1 < UBC1 && SEML1 < MAL1)
                        {
                            return CreateTrigger(context, 7.1498M, 7.1498M, basicTrigger);
                        }
                        else if (SEMC1 >= UBC1)
                        {
                            return CreateTrigger(context, 7.1449M, -7.1449M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.1419M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < SEMC13 && MAL2 < SEMC21)
                    {
                        return CreateTrigger(context, 7.1448M, -7.1448M, basicTrigger);
                    }
                    else if ((MAL2 < SEMC13 || MAL2 < SEMC21) && (MAL2 < SEMC5 || MAL2 < SEMC3 || MAL2 < SEMC2))
                    {
                        return CreateTrigger(context, 7.1447M, -7.1447M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1418M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAL2 < SEMC13)
            {
                if (MAL2 < SEMC2 && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 7.1446M, -7.1446M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1417M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 < UBC13 && SEMC21 > UBC21 && SEMC3 > MAC3)
            {
                if (MAL2 > SEMC3)
                {
                    return CreateTrigger(context, 7.1497M, 7.1497M, basicTrigger);
                }
                else if (MAL2 <= SEMC3)
                {
                    return CreateTrigger(context, 7.1445M, -7.1445M, basicTrigger);
                }
            }
            else if (MAL2 < UBC8)
            {
                if (MAC5 < MAC8)
                {
                    if (MAL2 > MAC3)
                    {
                        return CreateTrigger(context, 7.1496M, 7.1496M, basicTrigger);
                    }
                    else if (MAL2 <= MAC3 && MAC1 < UBC5)
                    {
                        return CreateTrigger(context, 7.1444M, -7.1444M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1416M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 < MAC3)
                {
                    if (MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 7.1495M, 7.1495M, basicTrigger, StartRule_7_1495);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1415M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 < SEMC5 && SEMC3 < SEMC5 && SEMC3 > UBC3 && MAL2 < MAC1)
                {
                    return CreateTrigger(context, 7.1443M, -7.1443M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1414M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= UBC8)
            {
                if (MAL2 >= UBC5)
                {
                    if (MAL2 > SEMC5)
                    {
                        if (C5_SEMTrend != -0.1M)
                        {
                            return CreateTrigger(context, 7.1413M, 0M, basicTrigger);
                        }
                        else if (MAL2 >= SEMC3)
                        {
                            if (SEMC1 < SEMC2 && SEMC1 < UB2C5)
                            {
                                return CreateTrigger(context, 7.1494M, 7.1494M, basicTrigger);
                            }
                            else if (SEMC1 > SEMC2 && SEMC1 > UBC1)
                            {
                                return CreateTrigger(context, 7.1442M, -7.1442M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.1412M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < SEMC3)
                        {
                            if (((MAL3 < MAL2 && MAL3 < MAL1) || MAL2 < MAL1) && SEMC2 < SEMC3 && SEMC1 < SEMC4 && SEMC4 > SEMC5 && SEMC3 > SEMC5 && SEMC2 > SEMC4)
                            {
                                return CreateTrigger(context, 7.1411M, 0M, basicTrigger);
                            }
                            else if (SEMC1 < SEMC2 && SEMC1 < SEMC3 && SEMC1 > SEMC4 && MAL2 < SEMC1)
                            {
                                return CreateTrigger(context, 7.1441M, 7.1441M, basicTrigger);
                            }
                            else if (SEMC2 < SEMC4 && SEMC1 < SEMC3 && SEMC1 < SEMC2 && SEMC1 > MAC1 && MAL2 < SEMC1)
                            {
                                return CreateTrigger(context, 7.1439M, -7.1439M, basicTrigger);
                            }
                            else if (SEMC2 > SEMC3 && SEMC1 < SEMC3 && SEMC1 < SEMC2 && SEMC1 > MAC1 && MAL2 < SEMC1)
                            {
                                return CreateTrigger(context, 7.1438M, -7.1438M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.1409M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 7.1408M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC2 > UBC5)
                    {
                        if (SEMC1 > MAC1 && MAL2 > MAC1 && MAL1 > MAC1 && MAC1 > UBC4 && SEMC8 > SEMC21)
                        {
                            return CreateTrigger(context, 7.1493M, 7.1493M, basicTrigger);
                        }
                        else if (SEMC1 < MAC1)
                        {
                            if (SEMC2 > UBC2 && SEMC3 > UBC3 && MAC1 > UBC4 && SEMC1 < MAC1)
                            {
                                return CreateTrigger(context, 7.1492M, 7.1492M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.1491M, 7.1491M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 7.1407M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC1 > UBC5)
                    {
                        if (SEMC1 > MAC1 && MAL2 > MAC1 && MAL1 > MAC1)
                        {
                            return CreateTrigger(context, 7.1489M, 7.1489M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.1406M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1405M, 0M, basicTrigger);
                    }
                }
                else if (MAC2 > UBC8)
                {
                    return CreateTrigger(context, 7.1404M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1403M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.13M)
        {
            if (MAL2 > SEMC3)
            {
                if (SEMC1 > SEMC2 && MAL2 > MAL1)
                {
                    return CreateTrigger(context, 7.1399M, 7.1399M, basicTrigger);
                }
                else if ((SEMC1 < SEMC2 || MAL2 < MAL1) && MAL2 < SEMC2 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 7.1349M, -7.1349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1319M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < SEMC2)
            {
                return CreateTrigger(context, 7.1348M, -7.1348M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.1318M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.12M)
        {
            if (SEMC2 > MAC2)
            {
                if (MAL2 < SEMC2 && (MAL2 < MAL1 || SEMC1 < SEMC2))
                {
                    return CreateTrigger(context, 7.1249M, -7.1249M, basicTrigger);
                }
                else if (SEMC3 > UBC3 && SEMC2 > UBC2 && MAL2 < SEMC2 && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 7.1248M, -7.1248M, basicTrigger);
                }
                else if (SEMC3 > MAC3 && SEMC2 > MAC2 && SEMC2 < SEMC3 && MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 7.1247M, -7.1247M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1219M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 <= MAC2)
            {
                if (MAC5 > MAC8)
                {
                    return CreateTrigger(context, 7.1299M, 7.1299M, basicTrigger, StartRule_7_1299);
                }
                else
                {
                    return CreateTrigger(context, 7.1218M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.11M)
        {
            if (SEMC5 > UB2C8)
            {
                if (MAL2 < SEMC5 && SEMC2 > UBC1 && MAL2 > UBC5 && LB2C1 < UBC4)
                {
                    return CreateTrigger(context, 7.1149M, -7.1149M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1119M, 0M, basicTrigger);
                }
            }
            else if (MAC5 < MAC8)
            {
                if ((MAL2 < SEMC5 || MAC2 < UBC5) && SEMC5 > UBC5 && MAL2 > UBC5 && SEMC8 < UBC8 && LB2C1 < UBC4)
                {
                    return CreateTrigger(context, 7.1148M, -7.1148M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1118M, 0M, basicTrigger);
                }
            }
            else if (LB2C1 > UBC4)
            {
                if (SEMC4 > UBC2 || SEMC4 > UBC1)
                {
                    return CreateTrigger(context, 7.1199M, 7.1199M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1117M, 0M, basicTrigger);
                }
            }
            else if (LBC1 > UBC4)
            {
                if (MAL2 > SEMC5 && SEMC5 > UBC5 && MAL2 > SEMC8 && SEMC8 > UBC8)
                {
                    return CreateTrigger(context, 7.1198M, 7.1198M, basicTrigger);
                }
                else if (MAL2 < SEMC5 && MAL2 < SEMC8 && MAC2 < UBC5)
                {
                    return CreateTrigger(context, 7.1147M, -7.1147M, basicTrigger);
                }
                else if (MAL2 < SEMC5 && MAC2 < UBC5)
                {
                    return CreateTrigger(context, 7.1146M, -7.1146M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1116M, 0M, basicTrigger);
                }
            }
            else if (MAC2 > UBC5)
            {
                if ((MAL2 > SEMC5 && SEMC5 > UBC5) || (MAL2 > SEMC8 && SEMC8 > UBC8))
                {
                    return CreateTrigger(context, 7.1197M, 7.1197M, basicTrigger);
                }
                else if (SEMC2 < SEMC5 && MAL2 > UBC5 && SEMC2 > UBC1 && SEMC5 > SEMC8 && MAL2 > SEMC8)
                {
                    return CreateTrigger(context, 7.1196M, 7.1196M, basicTrigger);
                }
                else if (SEMC1 < MAC2 && MAL2 < SEMC5 && SEMC2 > SEMC5)
                {
                    return CreateTrigger(context, 7.1145M, -7.1145M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1115M, 0M, basicTrigger);
                }
            }
            else if (MAC1 > UBC5)
            {
                if (MAL2 > UBC4)
                {
                    return CreateTrigger(context, 7.1195M, 7.1195M, basicTrigger);
                }
                else if (MAL2 > UBC5)
                {
                    if (MAC1 < SEMC5 && MAL2 < SEMC5 && SEMC2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.1144M, -7.1144M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.1114M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > UBC8 && MAL1 > UBC5)
                {
                    return CreateTrigger(context, 7.1143M, -7.1143M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.1113M, 0M, basicTrigger);
                }
            }
            else if (MAC1 < UBC5 && SEMC2 > UBC5 && MAL2 < SEMC5)
            {
                return CreateTrigger(context, 7.1142M, -7.1142M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.1112M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.06M)
        {
            if (SEMC1 < SEMC5)
            {
                return CreateTrigger(context, 7.0649M, -7.0649M, basicTrigger);
            }
            else if (SEMC8 <= UBC8)
            {
                if (MAL2 > UB2C5)
                {
                    if (SEMC1 > UBC1 && MAL2 < SEMC1 && MAL2 < SEMC2 && (SEML1 > MAL2 || SEML2 > MAL2))
                    {
                        return CreateTrigger(context, 7.0648M, -7.0648M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0619M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < SEMC21 && SEMC21 < UBC21)
                {
                    if (SEMC1 > MAC1 && MAL2 < SEMC1 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.0647M, -7.0647M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0618M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 < SEMC4 && SEMC3 < SEMC4)
                {
                    if (SEMC1 > MAC1 && SEMC2 > UBC2 && SEMC3 > UBC3 && MAL2 > UBC2 && SEMC1 < SEMC4)
                    {
                        return CreateTrigger(context, 7.0699M, 7.0699M, basicTrigger);
                    }
                    else if (SEMC1 > UBC1 && SEMC1 > SEMC4)
                    {
                        return CreateTrigger(context, 7.0646M, -7.0646M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0617M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 > MAC1)
                {
                    if (cParam.C5_SEM_LB2_Status == 0)
                    {
                        if (MAL2 > MAC1 && MAL2 > UBC4)
                        {
                            return CreateTrigger(context, 7.0698M, 7.0698M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.0616M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < UBC2)
                    {
                        return CreateTrigger(context, 7.0697M, 7.0697M, basicTrigger, StartRule_7_0697);
                    }
                    else if (MAL2 >= UBC2)
                    {
                        if (SEMC3 < SEMC5 && SEMC3 < UBC3 && SEMC2 > UBC2 && SEMC4 > UBC4)
                        {
                            return CreateTrigger(context, 7.0696M, 7.0696M, basicTrigger);
                        }
                        else if (SEML1 > MAL1)
                        {
                            if (SEML1 < UBC1 || SEMC1 < UBC1)
                            {
                                return CreateTrigger(context, 7.0695M, 7.0695M, basicTrigger);
                            }
                            else if (SEML1 > UBC1 && SEMC1 > UBC1 && SEML1 > UBL1)
                            {
                                return CreateTrigger(context, 7.0645M, -7.0645M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.0615M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC1 > SEMC2)
                        {
                            if (SEML1 < MAL1)
                            {
                                return CreateTrigger(context, 7.0694M, 7.0694M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 7.0614M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 7.0613M, 0M, basicTrigger);
                        }
                    }
                }
                else if (SEMC1 <= MAC1)
                {
                    return CreateTrigger(context, 7.0644M, -7.0644M, basicTrigger);
                }
            }
            else if (SEMC8 > UBC8)
            {
                if (MAL2 < UBC3)
                {
                    return CreateTrigger(context, 7.0643M, -7.0643M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && SEMC1 < UBC1)
                {
                    if (SEMC2 > UBC2 && SEMC3 > UBC3 && SEMC4 > UBC4 && SEMC1 > SEMC4)
                    {
                        return CreateTrigger(context, 7.0693M, 7.0693M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0642M, -7.0642M, basicTrigger);
                    }
                }
                else if (SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 7.0641M, -7.0641M, basicTrigger);
                }
                else if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 7.0692M, 7.0692M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.05M)
        {
            if (MAL2 > UB2C8)
            {
                return CreateTrigger(context, 7.0549M, -7.0549M, basicTrigger);
            }
            else if (MAL2 < SEMC8)
            {
                if (MAL2 > MAC3 && SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 7.0599M, 7.0599M, basicTrigger);
                }
                else if (MAL2 < MAC3)
                {
                    return CreateTrigger(context, 7.0548M, -7.0548M, basicTrigger);
                }
                else if (SEMC2 > MAC2)
                {
                    return CreateTrigger(context, 7.0547M, -7.0547M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0519M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 > UB2C8)
            {
                if (MAL2 > MAC1 && MAL1 > MAC1 && SEMC1 > MAC1 && SEMC3 > SEMC5)
                {
                    return CreateTrigger(context, 7.0598M, 7.0598M, basicTrigger);
                }
                else if (MAL2 < SEMC1 || SEMC2 > SEMC3 || (MAL2 < MAC1 && MAL1 < MAC1))
                {
                    return CreateTrigger(context, 7.0546M, -7.0546M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0518M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 > UB2C8)
            {
                if (SEMC2 > UBC2 && SEMC1 > MAC1 && (SEMC1 < UBC1 || SEMC1 < SEMC2))
                {
                    return CreateTrigger(context, 7.0545M, -7.0545M, basicTrigger);
                }
                else if (SEMC1 > SEMC8 && MAL2 > SEMC8)
                {
                    if (SEMC1 < MAC1 || SEMC2 < MAC2)
                    {
                        return CreateTrigger(context, 7.0597M, 7.0597M, basicTrigger);
                    }
                    else if (MAL2 < SEMC1 && MAL2 < SEMC2 && (SEMC1 < UBC1 || SEMC1 < SEMC2))
                    {
                        return CreateTrigger(context, 7.0544M, -7.0544M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0517M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 > MAC2 && SEMC1 > LBC1)
                {
                    return CreateTrigger(context, 7.0543M, -7.0543M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0516M, 0M, basicTrigger);
                }
            }
            else
            {
                if (SEMC1 > SEMC8 && MAL2 > MAC2 && (MAL2 > MAL1 || MAL1 > MAC1))
                {
                    return CreateTrigger(context, 7.0596M, 7.0596M, basicTrigger, StartRule_7_0596);
                }
                else if (MAL2 < MAL1 && MAL1 < MAC1)
                {
                    return CreateTrigger(context, 7.0542M, -7.0542M, basicTrigger);
                }
                else if (SEMC1 < SEMC8)
                {
                    return CreateTrigger(context, 7.0541M, -7.0541M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0515M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.04M)
        {
            if (LBC1 >= UBC3)
            {
                if (SEMC2 < UB2C5 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 7.0499M, 7.0499M, basicTrigger);
                }
                else if (C1_UBLB2_Dir == -1 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 7.0449M, -7.0449M, basicTrigger);
                }
                else if (SEMC1 < SEMC4 && SEMC1 < SEMC5)
                {
                    return CreateTrigger(context, 7.0448M, -7.0448M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0419M, 0M, basicTrigger);
                }
            }
            else if (LBC1 < UBC3)
            {
                if (C1_UBLB2_Dir == -1)
                {
                    return CreateTrigger(context, 7.0447M, -7.0447M, basicTrigger);
                }
                else if (C1_UBLB2_Dir == -1 && SEMC1 < UBC1 && (MAL2 < SEMC1 || MAL2 < MAL1))
                {
                    return CreateTrigger(context, 7.0446M, -7.0446M, basicTrigger);
                }
                else if (LBC1 < UBC3 && (SEMC3 < UB2C8 || SEMC2 < UB2C8))
                {
                    return CreateTrigger(context, 7.0445M, -7.0445M, basicTrigger);
                }
                else if (SEMC1 < SEMC4 && SEMC1 < SEMC5 && SEMC3 > SEMC5)
                {
                    return CreateTrigger(context, 7.0444M, -7.0444M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0418M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.01M)
        {
            if (SEMC1 > SEMC2 && SEMC3 > SEMC5)
            {
                if ((SEML1 > SEMC1 || SEML1 > SEMC2) && (SEML2 > SEMC1 || SEML2 > SEMC2))
                {
                    return CreateTrigger(context, 7.0199M, 7.0199M, basicTrigger);
                }
                else if (SEML1 < MAL2 && SEML1 < MAL1 && SEML2 < MAL2 && SEML2 < MAL1)
                {
                    return CreateTrigger(context, 7.0198M, 7.0198M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.0119M, 0M, basicTrigger);
                }
            }
            else if (UBC1 < UB2C8)
            {
                return CreateTrigger(context, 7.0149M, -7.0149M, basicTrigger);
            }
            else if (SEMC1 < SEMC2 && SEMC3 < SEMC5)
            {
                return CreateTrigger(context, 7.0148M, -7.0148M, basicTrigger);
            }
            else if (SEMC1 < SEMC2 && SEML1 < SEMC3 && SEML1 < SEMC2 && SEML1 < SEMC1 && SEML1 < SEMC3 && SEML2 < SEMC2 && SEML2 < SEMC1)
            {
                return CreateTrigger(context, 7.0147M, -7.0147M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.0118M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.79M)
        {
            if (SEMC13 > LBC13 && SEMC21 > LBC21 && MAL2 > UBC5 && SEMC1 > MAC1 && C1_SEMTrend == -0.1M && LB2C1 < MAC2)
            {
                return CreateTrigger(context, 6.7949M, -6.7949M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.7919M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.77M)
        {
            if (SEMC5 < SEMC8 && SEMC5 < SEMC13 && SEMC5 < SEMC21 && SEMC5 > UBC5 && SEMC8 > UBC8 && SEMC13 > UBC13 && SEMC21 > UBC21)
            {
                if (MAL2 > UBC5 && SEMC1 > SEMC5 && MAL2 < MAC1)
                {
                    return CreateTrigger(context, 6.7749M, -6.7749M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7719M, 0M, basicTrigger);
                }
            }
            else if (MAC1 > UB2C8)
            {
                if (MAL2 > UB2C8)
                {
                    return CreateTrigger(context, 6.7799M, 6.7799M, basicTrigger);
                }
                else if (MAL2 < UB2C8 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 6.7748M, -6.7748M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7718M, 0M, basicTrigger);
                }
            }
            else if (LB2C1 > SEMC5)
            {
                if (MAL2 > UBC3 && SEMC8 > UBC8)
                {
                    return CreateTrigger(context, 6.7798M, 6.7798M, basicTrigger);
                }
                else if (MAL2 < UBC3)
                {
                    return CreateTrigger(context, 6.7747M, -6.7747M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7717M, 0M, basicTrigger);
                }
            }
            else if (C1_UBLB2_Dir == 1)
            {
                if ((MAL2 > UBC3 || MAL1 > UBC3) && (SEMC2 < UB2C5 || SEMC3 < UB2C5) && SEMC8 < UB2C5)
                {
                    return CreateTrigger(context, 6.7797M, 6.7797M, basicTrigger);
                }
                else if (MAL2 < UBC3 && MAL1 < UBC3)
                {
                    return CreateTrigger(context, 6.7746M, -6.7746M, basicTrigger);
                }
                else if (SEMC2 > UB2C5 && SEMC3 > UB2C5)
                {
                    return CreateTrigger(context, 6.7745M, -6.7745M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7716M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < UBC3 && MAL1 < UBC3)
            {
                return CreateTrigger(context, 6.7744M, -6.7744M, basicTrigger);
            }
            else if (LB2C1 > UBC4)
            {
                return CreateTrigger(context, 6.7796M, 6.7796M, basicTrigger);
            }
            else if (MAL2 < MAC1)
            {
                return CreateTrigger(context, 6.7743M, -6.7743M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.7715M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.76M)
        {
            if (MAL2 > UBC5)
            {
                if (SEMC1 > SEMC5 && SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 6.7649M, -6.7649M, basicTrigger);
                }
                else if (SEMC1 <= SEMC5)
                {
                    return CreateTrigger(context, 6.7699M, 6.7699M, basicTrigger);
                }
                else if (SEMC1 < UBC1 && SEMC1 > SEMC3 && MAL2 > MAC1 && SEMC3 > SEMC5)
                {
                    return CreateTrigger(context, 6.7695M, 6.7695M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7619M, 0M, basicTrigger);
                }
            }
            else if (UB2C1 < SEMC5)
            {
                return CreateTrigger(context, 6.7648M, -6.7648M, basicTrigger);
            }
            else if (MAL2 <= MAC5)
            {
                if (SEMC2 < UBC5 && SEMC3 < UBC5 && SEMC4 < UBC5)
                {
                    return CreateTrigger(context, 6.7698M, 6.7698M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7618M, 0M, basicTrigger);
                }
            }
            else if (MAL2 <= UBC5)
            {
                if (MAL2 < MAC2 && SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 6.7697M, 6.7697M, basicTrigger, additionalStopCondition: StopRule_6_7697);
                }
                else if (MAL2 > MAC2)
                {
                    if (MAL2 > UBC8)
                    {
                        return CreateTrigger(context, 6.7696M, 6.7696M, basicTrigger, additionalStopCondition: StopRule_6_7696);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.7617M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < MAC2 && SEMC2 > MAC2)
                {
                    return CreateTrigger(context, 6.7647M, -6.7647M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7616M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.75M)
        {
            if (MAL2 <= UB2C8)
            {
                return CreateTrigger(context, 6.7549M, -6.7549M, basicTrigger);
            }
            else if (SEMC5 >= UB2C8)
            {
                if (SEMC1 >= MAC1)
                {
                    return CreateTrigger(context, 6.7548M, -6.7548M, basicTrigger);
                }
                else if (SEMC1 < MAC1)
                {
                    if (MAL2 < SEMC5)
                    {
                        return CreateTrigger(context, 6.7547M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.7519M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC1 < MAC1 && (MAL2 < SEMC5 || SEMC1 < SEMC5 || SEMC5 < UBC5))
            {
                return CreateTrigger(context, 6.7546M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC3 && (MAL2 > UB2C5 || MAL1 > UB2C5))
            {
                return CreateTrigger(context, 6.7545M, 0M, basicTrigger);
            }
            else if (MAL2 > SEMC3)
            {
                if (MAL2 > MAL1 && (SEMC2 < UB2C5 || SEMC3 < UB2C5))
                {
                    return CreateTrigger(context, 6.7599M, 6.7599M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7518M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 > MAC1 && SEMC1 < UB2C5)
            {
                return CreateTrigger(context, 6.7598M, 6.7598M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.7517M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.74M)
        {
            if (MAL3 < UBC3 && (MAL1 < UBC4 || MAL1 < UBC5 || MAL2 < UBC4 || MAL2 < UBC5 || MAL3 < UBC4 || MAL3 < UBC5))
            {
                if ((SEMC1 < SEMC3 || SEMC1 > MAC1) && C1_UBLB2_Dir == -1)
                {
                    return CreateTrigger(context, 6.7449M, -6.7449M, basicTrigger);
                }
                else if (SEMC1 < SEMC3 || SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 6.7448M, -6.7448M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7419M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 < UBC3)
            {
                if (MAL2 > UBC2 && SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 6.7499M, 6.7499M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7418M, 0M, basicTrigger);
                }
            }
            else if (MAC4 < MAC5)
            {
                if (SEMC1 > SEMC3)
                {
                    if (MAL2 > SEMC2)
                    {
                        return CreateTrigger(context, 6.7498M, 6.7498M, basicTrigger);
                    }
                    else if (MAL2 > UBC2)
                    {
                        if (SEMC1 > UBC1 && MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 6.7447M, -6.7447M, basicTrigger);
                        }
                        else if (SEMC1 < UBC1 && MAL2 < SEMC3)
                        {
                            return CreateTrigger(context, 6.7497M, 6.7497M, basicTrigger);
                        }
                        else if (SEMC1 > UBC1 && MAL2 < SEMC3)
                        {
                            return CreateTrigger(context, 6.7496M, 6.7496M, basicTrigger);
                        }
                        else if (SEMC1 < UBC1 && MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 6.7446M, -6.7446M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.7417M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 > SEMC3 || MAL1 > SEMC3)
                    {
                        return CreateTrigger(context, 6.7495M, 6.7495M, basicTrigger, StartRule_6_7495);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.7416M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 6.7494M, 6.7494M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7445M, -6.7445M, basicTrigger);
                }
            }
            else if (SEMC1 > MAC1 && SEMC3 > MAC1 && (SEMC2 > UBC1 || SEMC3 > UBC1) && (SEMC1 > SEMC2 || SEMC2 > SEMC3))
            {
                return CreateTrigger(context, 6.7493M, 6.7493M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.7415M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.73M)
        {
            if (UB2C5 > UB2C8 && UB2C8 > UB2C13 && SEMC5 > UBC5 && SEMC8 > UBC8 && SEMC13 > UBC13)
            {
                if ((C13_SEMTrend == 1.1M || C8_SEMTrend == 1.1M || C5_SEMTrend == 1.1M) && (MAL2 > UBC2 || MAL1 > UBC2) && SEMC1 < UB2C5 && SEMC1 > UBC1 && MAL2 < UB2C8 && MAL2 < UB2C5)
                {
                    return CreateTrigger(context, 6.7399M, 6.7399M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7319M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > UBC2 || MAL1 > UBC2)
            {
                if (MAL2 > SEMC4 && MAL2 > MAC1 && MAL1 > MAC1 && SEMC1 > MAC1)
                {
                    if (SEML1 < UBC1 && SEML2 < UBC1 && MAL3 < UBC1)
                    {
                        return CreateTrigger(context, 6.7398M, 6.7398M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.7318M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 < SEMC4 && MAL2 < MAL1)
                {
                    return CreateTrigger(context, 6.7349M, -6.7349M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7317M, 0M, basicTrigger);
                }
            }
            else if (MAC1 > UB2C8 && MAL2 > UB2C8)
            {
                if (SEMC1 > SEMC5 && SEMC1 < MAC1 && SEMC5 > UBC5)
                {
                    return CreateTrigger(context, 6.7397M, 6.7397M, basicTrigger);
                }
                else if (SEMC5 < UBC5)
                {
                    return CreateTrigger(context, 6.7348M, -6.7348M, basicTrigger);
                }
                else if (SEMC5 > UBC5 && SEMC1 > MAC1 && MAL2 < SEMC1)
                {
                    return CreateTrigger(context, 6.7347M, -6.7347M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7346M, -6.7346M, basicTrigger);
                }
            }
            else if (SEMC5 < UBC5)
            {
                return CreateTrigger(context, 6.7345M, -6.7345M, basicTrigger);
            }
            else if (SEMC5 > UBC5 && SEMC1 > MAC1 && MAL2 < SEMC1)
            {
                return CreateTrigger(context, 6.7344M, -6.7344M, basicTrigger);
            }
            else if (SEMC1 < MAC1 && SEMC1 > SEMC5 && SEMC5 > UBC5 && SEMC5 > SEMC8)
            {
                return CreateTrigger(context, 6.7396M, 6.7396M, basicTrigger, StartRule_6_7396);
            }
            else
            {
                return CreateTrigger(context, 6.7316M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.72M)
        {
            if (SEMC5 >= MAC5)
            {
                if (SEML2 > SEMC5 || SEML1 > SEMC5 || SEMC1 > SEMC5)
                {
                    return CreateTrigger(context, 6.7299M, 6.7299M, basicTrigger);
                }
                else if (SEML1 < SEMC1 || SEML2 < SEMC1)
                {
                    return CreateTrigger(context, 6.7249M, -6.7249M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.7219M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.65M)
        {
            if (MAC3 < MAC8 && MAC3 < UBC21 && MAC2 < MAC8 && MAC1 < MAC8)
            {
                if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 6.6549M, -6.6549M, basicTrigger);
                }
                else if (SEMC1 <= MAC1 && (MAL2 < SEMC3 || MAL2 < SEMC2))
                {
                    return CreateTrigger(context, 6.6548M, -6.6548M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6519M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC5 && MAL2 > LBC5)
            {
                if (MAC3 < MAC8 && MAC3 < UBC21)
                {
                    if (MAL2 > SEMC5 && SEMC3 > SEMC5 && SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 6.6599M, 6.6599M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6518M, 0M, basicTrigger);
                    }
                }
                else if (SEMC4 < MAC5 && SEMC3 < MAC5 && SEMC2 < MAC5)
                {
                    if (SEMC1 < MAC1 && MAL2 < MAC3)
                    {
                        return CreateTrigger(context, 6.6598M, 6.6598M, basicTrigger);
                    }
                    else if (SEMC1 > MAC1 && SEMC3 < MAC3 && MAL1 > MAC1 && MAL2 > MAC1 && MAL2 > SEMC3)
                    {
                        if (MAL2 < MAC3)
                        {
                            if (MAL2 > SEMC5 || SEMC1 > SEMC5)
                            {
                                return CreateTrigger(context, 6.6597M, 6.6597M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6517M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 > MAC3 && SEMC3 > SEMC5)
                        {
                            return CreateTrigger(context, 6.6596M, 6.6596M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6516M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6547M, -6.6547M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.6546M, -6.6546M, basicTrigger);
                }
            }
            else if (MAL2 <= LBC5)
            {
                if (MAL2 < LBC8)
                {
                    if (MAL2 > LB2C5)
                    {
                        return CreateTrigger(context, 6.6545M, -6.6545M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6515M, 0M, basicTrigger);
                    }
                }
                else if (SEMC13 < UBC8 && SEMC8 < UBC8 && SEMC8 < SEMC13)
                {
                    return CreateTrigger(context, 6.6544M, -6.6544M, basicTrigger);
                }
                else if (SEMC4 < LBC4)
                {
                    if (SEMC5 < LBC5)
                    {
                        return CreateTrigger(context, 6.6595M, 6.6595M, basicTrigger);
                    }
                    else if (SEMC2 < LBC5 && SEMC3 < LBC5)
                    {
                        return CreateTrigger(context, 6.6594M, 6.6594M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6514M, 0M, basicTrigger);
                    }
                }
                else if (SEMC4 > LBC4)
                {
                    return CreateTrigger(context, 6.6543M, -6.6543M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6513M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > MAC5)
            {
                return CreateTrigger(context, 6.6512M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.6511M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.64M)
        {
            if (MAL2 > UBC21 && (MAL2 < MAC8 || MAL2 < MAC5))
            {
                if (SEMC1 > MAC1)
                {
                    if (SEML2 >= MAL2)
                    {
                        return CreateTrigger(context, 6.6449M, -6.6449M, basicTrigger);
                    }

                    if (SEML2 < MAL2)
                    {
                        return CreateTrigger(context, 6.6448M, -6.6448M, basicTrigger, StartRule_6_6448);
                    }
                }

                if (SEMC1 <= MAC1)
                {
                    return CreateTrigger(context, 6.6447M, -6.6447M, basicTrigger, StartRule_6_6447);
                }
            }
            else if (SEMC8 < MAC13)
            {
                if (MAL2 < MAC13 && MAL2 < MAC1 && MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 6.6446M, -6.6446M, basicTrigger);
                }
                else if (SEMC13 < UBC8 && SEMC13 >= UBC13 && MAL2 > SEMC5 && MAL2 > MAC13)
                {
                    return CreateTrigger(context, 6.6499M, 6.6499M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6419M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 >= UBC13)
            {
                if (MAL2 >= UBC21 && MAL2 > MAC8 && MAL2 > MAC5)
                {
                    if (MAL2 > UBC5)
                    {
                        return CreateTrigger(context, 6.6418M, 0M, basicTrigger);
                    }
                    else if (MAL2 <= UBC5)
                    {
                        if (SEMC1 < MAC1)
                        {
                            return CreateTrigger(context, 6.6498M, 6.6498M, basicTrigger);
                        }
                        else if (SEMC1 < UBC1 && MAL1 > MAC1 && MAL2 > MAC1)
                        {
                            return CreateTrigger(context, 6.6497M, 6.6497M, basicTrigger);
                        }
                        else if (SEMC1 > UBC1)
                        {
                            return CreateTrigger(context, 6.6445M, -6.6445M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6417M, 0M, basicTrigger);
                        }
                    }
                }
                else if (MAL2 < UBC21)
                {
                    if (SEMC13 < UBC21)
                    {
                        return CreateTrigger(context, 6.6416M, 0M, basicTrigger);
                    }
                    else if (SEMC13 < UBC8)
                    {
                        if (MAL2 > SEMC5 && MAL2 > MAC13)
                        {
                            if (MAL2 < MAC5 && SEMC1 < MAC1)
                            {
                                return CreateTrigger(context, 6.6496M, 6.6496M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6415M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6444M, -6.6444M, basicTrigger);
                        }
                    }
                    else if (MAL2 <= MAC13)
                    {
                        if (MAL2 > LB2C5 && MAL2 < LB2C8)
                        {
                            if (SEMC5 < LBC5 && (MAL3 > SEMC1 || MAL2 > SEMC2))
                            {
                                return CreateTrigger(context, 6.6495M, 6.6495M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6414M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC13 <= SEMC21)
                        {
                            if (SEMC5 > LBC5 && SEMC5 > LBC8 && SEMC5 > MAC13)
                            {
                                return CreateTrigger(context, 6.6494M, 6.6494M, basicTrigger);
                            }
                            else if (SEMC5 < LBC5 && SEMC5 < LBC8 && SEMC5 < MAC13)
                            {
                                return CreateTrigger(context, 6.6493M, 6.6493M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6413M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < LB2C5 || MAL2 < LB2C8)
                        {
                            return CreateTrigger(context, 6.6412M, 0M, basicTrigger);
                        }
                        else if (SEMC13 > SEMC21)
                        {
                            if (MAL2 < SEMC2 && MAL2 < SEMC5)
                            {
                                return CreateTrigger(context, 6.6443M, -6.6443M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6411M, 0M, basicTrigger);
                            }
                        }
                    }
                    else if (MAL2 > LB2C5 && MAL2 < LB2C8)
                    {
                        if (MAL3 > SEMC1 || MAL2 > SEMC2)
                        {
                            return CreateTrigger(context, 6.6492M, 6.6492M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6409M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC5 > UBC21)
                    {
                        if (MAC5 < MAC8)
                        {
                            if (MAL2 > LBC5 && MAL2 < MAC5)
                            {
                                return CreateTrigger(context, 6.6442M, -6.6442M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6408M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < MAC5 && MAL2 > LBC5)
                        {
                            if (SEMC1 <= MAC1 && SEMC2 < UBC2)
                            {
                                if ((MAC1 > MAC2 && MAC2 > MAC3) || (SEMC8 > UBC21 && SEMC5 < LBC5 && SEMC1 > SEMC5))
                                {
                                    return CreateTrigger(context, 6.6491M, 6.6491M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 6.6407M, 0M, basicTrigger);
                                }
                            }
                            else if (SEMC1 > MAC1 || SEMC2 > UBC2)
                            {
                                return CreateTrigger(context, 6.6441M, -6.6441M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 6.6406M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < LBC5)
                        {
                            if (SEMC5 < LBC5)
                            {
                                if (SEMC8 < UBC21)
                                {
                                    if (SEMC2 > SEMC4 || SEMC3 > SEMC4)
                                    {
                                        return CreateTrigger(context, 6.6489M, 6.6489M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 6.6439M, -6.6439M, basicTrigger);
                                    }
                                }
                                else if (SEMC3 > SEMC5)
                                {
                                    return CreateTrigger(context, 6.6488M, 6.6488M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 6.6438M, -6.6438M, basicTrigger);
                                }
                            }
                            else if (SEMC5 >= LBC5)
                            {
                                return CreateTrigger(context, 6.6405M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 6.6404M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 6.6403M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC13 < UBC13)
            {
                if (SEMC8 > MAC13)
                {
                    return CreateTrigger(context, 6.6487M, 6.6487M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6437M, -6.6437M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.56M)
        {
            if (SEMC13 > UBC21 && SEMC13 > SEMC21)
            {
                if (MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 6.5649M, -6.5649M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5619M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < LB2C13 && MAL2 < LB2C21)
            {
                if (MAL2 < SEMC5)
                {
                    return CreateTrigger(context, 6.5648M, -6.5648M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5618M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 < MAC13 && SEMC21 < MAC21)
            {
                if (MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 6.5647M, -6.5647M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5617M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < LB2C21)
            {
                if (MAL2 < SEMC3)
                {
                    return CreateTrigger(context, 6.5646M, -6.5646M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5616M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC13 && SEMC5 > LBC5)
            {
                return CreateTrigger(context, 6.5645M, -6.5645M, basicTrigger);
            }
            else if (SEMC3 < LB2C8 && MAL2 < MAC21)
            {
                return CreateTrigger(context, 6.5699M, 6.5699M, basicTrigger);
            }
            else if (SEMC3 > LB2C8 && MAL2 > MAC21 && SEMC2 < LB2C8)
            {
                return CreateTrigger(context, 6.5698M, 6.5698M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.5615M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.52M)
        {
            if (MAC5 < MAC8)
            {
                if (SEMC5 > UBC5 && MAC1 > UBC5 && MAL2 > UBC5 && SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 6.5249M, -6.5249M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5219M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < UBC5)
            {
                if (SEMC3 >= UBC8)
                {
                    if (MAC2 < UBC5 && SEMC2 < MAC2)
                    {
                        return CreateTrigger(context, 6.5299M, 6.5299M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.5248M, -6.5248M, basicTrigger);
                    }
                }
                else if (SEMC3 < UBC8)
                {
                    return CreateTrigger(context, 6.5218M, 0M, basicTrigger);
                }
            }
            else if (MAC3 > UBC8 && MAC2 > UBC5)
            {
                if (MAC1 >= MAC2 && MAL2 > UBC5)
                {
                    if (MAL2 > MAC1 && MAL1 > MAC1 && MAL3 > MAC1 && SEMC1 > MAC1 && SEML1 > MAC1 && SEML2 > MAC1)
                    {
                        return CreateTrigger(context, 6.5247M, -6.5247M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.5298M, 6.5298M, basicTrigger);
                    }
                }
                else if (MAC1 < MAC2 && SEMC2 < SEMC8 && SEMC1 < SEMC2 && SEMC1 < UBC5)
                {
                    return CreateTrigger(context, 6.5246M, -6.5246M, basicTrigger);
                }
                else if (MAC1 > UBC5 && MAC2 > UBC5 && SEMC2 > SEMC8 && SEMC1 > UBC5 && SEMC2 > UBC5)
                {
                    return CreateTrigger(context, 6.5297M, 6.5297M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5217M, 0M, basicTrigger);
                }
            }
            else if (MAC3 > UBC8 && MAC2 < UBC5)
            {
                if (MAL2 > UBC5 && MAL1 > UBC5 && MAC1 > UBC5 && SEMC1 < MAC1 && MAC1 > MAC2)
                {
                    return CreateTrigger(context, 6.5296M, 6.5296M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5216M, 0M, basicTrigger);
                }
            }
            else if (MAC3 < UBC8 && MAC2 > UBC5)
            {
                if (SEMC8 > UBC8 && MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 6.5295M, 6.5295M, basicTrigger);
                }
                else if (SEMC5 > UBC5 && C5_SEMTrend == -0.1M && SEMC4 > UBC4 && C4_SEMTrend == -0.1M && SEMC3 > UBC3 && C3_SEMTrend == -0.1M && SEMC2 > UBC2 && C2_SEMTrend == -0.1M && C8_SEMTrend != -0.1M && SEMC8 > SEMC13 && MAL2 > SEMC8 && MAL2 > UBC8 && SEMC8 > UBC8)
                {
                    return CreateTrigger(context, 6.5245M, -6.5245M, basicTrigger);
                }
                else if (SEMC8 > UBC8 && MAL2 > SEMC8)
                {
                    return CreateTrigger(context, 6.5244M, -6.5244M, basicTrigger);
                }
                else if (SEMC8 < UBC8)
                {
                    return CreateTrigger(context, 6.5243M, -6.5243M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5215M, 0M, basicTrigger);
                }
            }
            else if (MAC3 < UBC8 && MAC2 < UBC5)
            {
                if (MAC1 > UBC5 && SEMC1 > MAC1 && MAL2 > UBC5 && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC5 && SEMC2 < SEMC3 && SEMC3 < SEMC5)
                {
                    return CreateTrigger(context, 6.5242M, -6.5242M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5214M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 6.5213M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.51M)
        {
            if (MAL2 > UB2C13 && MAL2 > UB2C21 && MAC1 <= UB2C8)
            {
                return CreateTrigger(context, 6.5148M, -6.5148M, basicTrigger);
            }
            else if (MAC5 > UBC13 && MAC8 > UBC21 && SEMC5 > MAC5 && SEMC8 > UBC8 && SEMC13 > UBC13 && SEMC21 > UBC21)
            {
                if (SEMC5 <= UBC5)
                {
                    return CreateTrigger(context, 6.5199M, 6.5199M, basicTrigger);
                }
                else if (SEMC5 > UBC5)
                {
                    if (SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 6.5198M, 6.5198M, basicTrigger, additionalStopCondition: StopRule_6_5198);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.5119M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAC3 > UB2C21)
            {
                return CreateTrigger(context, 6.5197M, 6.5197M, basicTrigger);
            }
            else if (MAC2 > UBC5)
            {
                if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 6.5149M, -6.5149M, basicTrigger, StartRule_6_5149);
                }
                else
                {
                    return CreateTrigger(context, 6.5147M, -6.5147M, basicTrigger);
                }
            }
            else if (LBC1 < MAC3)
            {
                return CreateTrigger(context, 6.5146M, -6.5146M, basicTrigger);
            }
            else if (LB2C1 < MAC3)
            {
                return CreateTrigger(context, 6.5145M, -6.5145M, basicTrigger);
            }
            else if (MAC2 < MAC3)
            {
                return CreateTrigger(context, 6.5144M, -6.5144M, basicTrigger);
            }
            else if (MAC2 > MAC3 && MAC2 > MAC5)
            {
                if (SEMC5 > UB2C1)
                {
                    return CreateTrigger(context, 6.5143M, -6.5143M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && SEMC2 > MAC1 && SEMC3 > MAC1 && SEMC4 > MAC1 && SEMC5 > MAC1)
                {
                    return CreateTrigger(context, 6.5142M, -6.5142M, basicTrigger);
                }
                else if (SEMC1 < UBC3 || MAL1 < UBC3 || MAL2 < UBC3 || MAL3 < UBC3)
                {
                    if ((SEMC3 < UBC3 && SEMC1 < UBC3) || SEMC1 < SEMC3)
                    {
                        return CreateTrigger(context, 6.5141M, -6.5141M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.5118M, 0M, basicTrigger);
                    }
                }
                else if (MAC1 > UBC3 && MAL1 > UBC3 && MAL2 > UBC3 && SEMC1 < SEMC2 && (SEMC3 > UBC3 || SEMC3 > UBC5 || SEMC5 > UBC5))
                {
                    if (MAL1 > UBC2 || MAL2 > UBC2)
                    {
                        if (MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 6.5196M, 6.5196M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.5117M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL1 < UBC2 && MAL2 < UBC2)
                    {
                        return CreateTrigger(context, 6.5195M, 6.5195M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.5116M, 0M, basicTrigger);
                }
            }
            else if (MAC2 < MAC5 && MAC2 > MAC3)
            {
                if (LB2C1 < MAC5 && LBC1 < MAC5)
                {
                    if ((MAL2 > SEMC3 && SEMC3 > UBC3) || (C3_SEMTrend != -0.1M && SEMC3 > UBC3))
                    {
                        return CreateTrigger(context, 6.5194M, 6.5194M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.5115M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 6.5139M, -6.5139M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5138M, -6.5138M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 6.5114M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.48M)
        {
            if (SEMC8 < LBC8)
            {
                if (MAL2 > LBC5 && MAL2 > SEMC8)
                {
                    return CreateTrigger(context, 6.4899M, 6.4899M, basicTrigger);
                }
                else if (MAL2 < LB2C5 || MAL2 < LB2C8)
                {
                    return CreateTrigger(context, 6.4898M, 6.4898M, basicTrigger);
                }
                else if (MAL2 < LBC5 || MAL2 < SEMC8)
                {
                    return CreateTrigger(context, 6.4849M, -6.4849M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4819M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < LBC5)
            {
                if (SEMC2 > LBC2 && SEMC3 > LBC3 && SEMC4 > LBC4)
                {
                    if (SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC4 < MAC4)
                    {
                        return CreateTrigger(context, 6.4897M, 6.4897M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4818M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 <= LBC2 && SEMC3 <= LBC3 && SEMC4 <= LBC4 && MAL2 >= SEMC2)
                {
                    if (SEMC4 < MAC1)
                    {
                        if (MAL2 < MAC1)
                        {
                            return CreateTrigger(context, 6.4896M, 6.4896M, basicTrigger);
                        }
                        else if (MAL2 > MAC1 && C1_SEMTrend != -0.1M)
                        {
                            return CreateTrigger(context, 6.4895M, 6.4895M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.4817M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4848M, -6.4848M, basicTrigger);
                    }
                }
                else if (SEMC2 < LBC2 && SEMC3 < LBC3 && SEMC4 < LBC4 && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 6.4847M, -6.4847M, basicTrigger);
                }
                else if (MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > SEMC4 && MAL2 > SEMC5 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC4 < MAC4)
                {
                    if (MAL2 > MAC1 && SEMC1 > MAC1 && C1_SEMTrend != -0.1M)
                    {
                        return CreateTrigger(context, 6.4894M, 6.4894M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4816M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > LB2C8 && MAL2 > LB2C5)
                {
                    return CreateTrigger(context, 6.4846M, -6.4846M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4815M, 0M, basicTrigger);
                }
            }
            else if (MAL2 >= LBC5)
            {
                if (SEMC1 > MAC1 && C1_SEMTrend != -0.1M)
                {
                    return CreateTrigger(context, 6.4893M, 6.4893M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && C1_SEMTrend == -0.1M && SEMC1 > UBC1 && MAL2 > MAL1 && MAL1 > MAC1 && SEML2 < MAL2)
                {
                    return CreateTrigger(context, 6.4892M, 6.4892M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && C1_SEMTrend == -0.1M && MAL2 > MAL1 && MAL1 > MAC1 && SEML2 > MAL2 && MAL2 > SEMC2)
                {
                    return CreateTrigger(context, 6.4891M, 6.4891M, basicTrigger);
                }
                else if (MAC1 > MAC3 && SEMC1 < MAC1 && MAL2 > SEMC3)
                {
                    return CreateTrigger(context, 6.4889M, 6.4889M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4814M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.47M)
        {
            if (C21_MajorTrend == -1 || MAC5 < MAC89)
            {
                if (MAL2 < MAC5 || MAC1 < MAC5)
                {
                    return CreateTrigger(context, 6.4799M, 6.4799M, basicTrigger);
                }
                else
                {
                    if (MAL2 > MAL1)
                    {
                        return CreateTrigger(context, 6.4798M, 6.4798M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4797M, 6.4797M, basicTrigger);
                    }
                }
            }
            else if (MAC3 < MAC5)
            {
                if (MAL2 < MAC5)
                {
                    if (MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && MAC4 < MAC5 && MAL2 < MAC1 && (MAL2 < MAC2 || MAL2 < MAC3 || MAL2 < MAC4 && SEMC1 < MAC2 && SEMC1 < MAC3 && SEMC1 < MAC4 && SEMC1 < LBC1 && SEMC2 > MAC2 && SEMC3 > MAC3) && SEMC2 > SEMC5 && SEMC3 > SEMC5 && MAL2 > MAC13 && MAL2 > MAC21)
                    {
                        return CreateTrigger(context, 6.4796M, 6.4796M, basicTrigger);
                    }
                    else if (SEMC1 > LBC1 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 6.4749M, -6.4749M, basicTrigger);
                    }
                    else if (SEMC2 < MAC2 && SEMC3 < MAC3 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > SEMC3 && MAL2 < SEMC2 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 6.4748M, -6.4748M, basicTrigger);
                    }
                    else if (MAC1 > MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC4 > MAC4 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && SEMC2 < SEMC3 && SEMC2 < SEMC4)
                    {
                        return CreateTrigger(context, 6.4747M, -6.4747M, basicTrigger, StartRule_6_4747);
                    }
                    else if (MAL2 < MAC13 || MAL2 < MAC21 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && MAC4 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 6.4746M, -6.4746M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4719M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 > MAC5)
                {
                    if (cParam.L2_L1_MA_Conf == 1 && MAL2 > MAC1 && MAL1 > MAC1)
                    {
                        return CreateTrigger(context, 6.4795M, 6.4795M, basicTrigger);
                    }
                    else if (SEMC1 < MAC1 && MAL1 > MAC1 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1 && MAL2 < MAC1 && MAL2 > SEMC4 && MAC1 > MAC5 && SEMC2 > SEMC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5)
                    {
                        return CreateTrigger(context, 6.4794M, 6.4794M, basicTrigger);
                    }
                    else if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 6.4745M, -6.4745M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4718M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.4717M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 6.4716M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.46M)
        {
            return CreateTrigger(context, 6.4619M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.45M)
        {
            if (MAL2 > MAC2 && MAL2 < SEMC1 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5 && SEMC1 > MAC1 && SEMC1 < SEMC5 && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4)
            {
                return CreateTrigger(context, 6.4549M, -6.4549M, basicTrigger, StartRule_6_4549);
            }
            else if (MAL2 < MAC3 && MAL2 < C2_MAC_SupRes && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC5)
            {
                if (cClose > MAC2 || cHigh >= MAC2)
                {
                    return CreateTrigger(context, 6.4548M, -6.4548M, basicTrigger, StartRule_6_4548);
                }
                else
                {
                    return CreateTrigger(context, 6.4547M, -6.4547M, basicTrigger);
                }
            }
            else if (MAL2 < MAC2 && SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > C2_MAC_SupRes && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4 && SEMC3 < SEMC5 && SEMC3 > MAC3 && SEMC4 > MAC4 && SEMC5 > MAC5 && SEMC4 < SEMC5)
            {
                return CreateTrigger(context, 6.4599M, 6.4599M, basicTrigger);
            }
            else if (MAL2 < MAC2 && SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > MAC3 && MAL2 > LBC2 && C2_UBLB2_Dir == 1 && C3_UBLB1_Dir == -1 && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4 && SEMC3 < SEMC5 && SEMC4 > SEMC5)
            {
                return CreateTrigger(context, 6.4598M, 6.4598M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.4519M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.44M)
        {
            if (MAL2 > MAC5)
            {
                if (cParam.L2_L1_MA_Conf == 1 && MAL2 > MAC1 && MAL1 > MAC1)
                {
                    return CreateTrigger(context, 6.4499M, 6.4499M, basicTrigger);
                }
                else if (SEMC1 < MAC1 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1 && MAL2 < MAC1 && MAL2 > SEMC4 && MAC1 > MAC5 && SEMC3 > SEMC4 && SEMC4 > SEMC5 && SEMC5 > MAC5)
                {
                    return CreateTrigger(context, 6.4498M, 6.4498M, basicTrigger);
                }
                else if (MAL2 > MAC5 && MAL1 > MAC5 && MAC1 > MAC5 && MAC2 > MAC5 && MAL2 < MAC1 && MAL2 > MAC2 && MAL2 > MAC3 && MAL2 > MAC4 && MAL1 < MAC1 && SEMC1 < MAC1 && SEMC1 > MAC2 && SEMC1 > MAC3 && SEMC1 > MAC4 && SEMC1 > MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC2 > MAC5 && SEMC3 > MAC5 && SEMC4 > MAC5 && SEMC1 > SEMC5 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && SEMC1 > SEMC4 && SEMC2 > SEMC4 && SEMC3 > SEMC4 && SEMC5 < UBC5 && SEMC4 < UBC4 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1)
                {
                    return CreateTrigger(context, 6.4497M, 6.4497M, basicTrigger);
                }
                else if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                {
                    if (pC1_UBLB2_Dir == -1 || pL1_UBLB2_Dir == -1)
                    {
                        return CreateTrigger(context, 6.4449M, -6.4449M, basicTrigger);
                    }
                    else if (MAL2 < MAC2)
                    {
                        return CreateTrigger(context, 6.4448M, -6.4448M, basicTrigger, StartRule_6_4448);
                    }
                    else if (SEMC2 > SEMC3)
                    {
                        return CreateTrigger(context, 6.4447M, -6.4447M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.4446M, -6.4446M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.4419M, 0M, basicTrigger);
                }
            }
            else if (MAL2 <= MAC5)
            {
                if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                {
                    return CreateTrigger(context, 6.4445M, -6.4445M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4418M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.43M)
        {
            if (MAC1 > MAC5 && SEMC1 < LBC1 && MAL2 > SEMC1 && MAC2 > MAC5 && MAC3 > MAC4 && MAC3 > MAC5 && MAC4 > MAC5)
            {
                return CreateTrigger(context, 6.4399M, 6.4399M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.4319M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.42M)
        {
            if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && SEMC3 < SEMC4 && SEMC2 < SEMC3 && SEMC1 < LBC1)
            {
                return CreateTrigger(context, 6.4249M, -6.4249M, basicTrigger);
            }
            else if (MAL2 < MAC4 && MAL2 < MAC3 && (MAC1 > SEMC8 || MAC1 > SEMC13 || MAC1 > UBC13) && !(SEMC5 < SEMC8 && SEMC4 < SEMC5 && SEMC3 < SEMC4 && SEMC2 < SEMC3))
            {
                return CreateTrigger(context, 6.4299M, 6.4299M, basicTrigger);
            }
            else if (SEMC13 < SEMC21 && SEMC13 < UBC13 && C13_SEMTrend == -0.1M && SEMC1 < LBC1)
            {
                return CreateTrigger(context, 6.4248M, -6.4248M, basicTrigger);
            }
            else if (SEMC13 > SEMC21 && C1_SEMTrend != -0.1M && SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC5 > SEMC13 && SEMC3 > SEMC13 && MAC3 > SEMC13)
            {
                return CreateTrigger(context, 6.4298M, 6.4298M, basicTrigger);
            }
            else if (SEMC8 > SEMC13 && MAC5 > UBC13 && SEMC8 > UBC13 && SEMC13 > UBC13 && SEMC5 < SEMC8 && SEMC5 > UBC5 && SEMC4 < SEMC8 && SEMC3 < SEMC8 && SEMC2 < SEMC8 && MAC1 < UBC5)
            {
                return CreateTrigger(context, 6.4297M, 6.4297M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.4219M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.41M)
        {
            if (MAL2 <= LBC5)
            {
                if (SEMC1 < MAC1 && SEMC8 < LBC5)
                {
                    return CreateTrigger(context, 6.4199M, 6.4199M, basicTrigger);
                }
                else if (SEMC8 > LBC5)
                {
                    return CreateTrigger(context, 6.4149M, -6.4149M, basicTrigger);
                }
                else if (SEMC1 > MAC1 && C1_SEMTrend == -0.1M)
                {
                    return CreateTrigger(context, 6.4148M, -6.4148M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4119M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC5 && MAL2 > LBC5)
            {
                if (SEMC3 > UBC3 && SEMC5 > SEMC8)
                {
                    return CreateTrigger(context, 6.4198M, 6.4198M, basicTrigger);
                }
                else if (SEMC3 < UBC3 && SEMC5 < SEMC8)
                {
                    return CreateTrigger(context, 6.4147M, -6.4147M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.4118M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 6.4117M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.38M)
        {
            if (SEMC5 > UBC5 && MAL2 < SEMC3)
            {
                return CreateTrigger(context, 6.3849M, -6.3849M, basicTrigger);
            }
            else if (MAL1 < UB2C5)
            {
                return CreateTrigger(context, 6.3848M, -6.3848M, basicTrigger);
            }
            else if (MAL1 >= UB2C5)
            {
                if (SEMC5 <= UBC5)
                {
                    if (SEMC4 < UBC4)
                    {
                        return CreateTrigger(context, 6.3899M, 6.3899M, basicTrigger);
                    }
                    else if (C3_SEMTrend == -0.1M && MAL2 < MAL1)
                    {
                        return CreateTrigger(context, 6.3847M, -6.3847M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.3819M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 > UBC5)
                {
                    if (MAL2 > SEMC3 && SEMC3 < UB2C5 && MAL2 > SEMC8)
                    {
                        if (MAL2 >= UB2C4)
                        {
                            return CreateTrigger(context, 6.3818M, 0M, basicTrigger);
                        }
                        else if (SEMC1 > SEMC2 && MAL3 > MAL2 && MAL2 > MAL1 && SEML2 < SEML1)
                        {
                            return CreateTrigger(context, 6.3898M, 6.3898M, basicTrigger);
                        }
                        else if (SEML2 < SEML1 || MAL3 < MAL2 || MAL3 < MAL1 || MAL2 < MAL3)
                        {
                            return CreateTrigger(context, 6.3846M, -6.3846M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 6.3817M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 6.3845M, -6.3845M, basicTrigger);
                    }
                }
            }
        }

        if (basicTrigger == 6.37M)
        {
            return CreateTrigger(context, 6.3749M, -6.3749M, basicTrigger);
        }

        if (basicTrigger == 6.36M)
        {
            if (SEMC1 <= MAC1)
            {
                if (MAL2 < LBC5)
                {
                    return CreateTrigger(context, 6.3699M, 6.3699M, basicTrigger);
                }
                else if (MAL2 < MAC1 && MAL1 < MAC1)
                {
                    return CreateTrigger(context, 6.3698M, 6.3698M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.3619M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 > MAC1)
            {
                return CreateTrigger(context, 6.3649M, -6.3649M, basicTrigger);
            }
        }

        if (basicTrigger == 6.35M)
        {
            if (SEMC1 <= MAC1)
            {
                return CreateTrigger(context, 6.3599M, 6.3599M, basicTrigger);
            }
            else if (SEMC1 > MAC1)
            {
                return CreateTrigger(context, 6.3519M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.29M)
        { // done

            // && MAC13 > MAC21 && MAC21 > MAC34
            // && (LB2C13 > MAC34 || LBC21 > MAC34)
            // && SEMC13 < MAC13 && SEMC21 > MAC21) {   


            if (SEMC5 < MAC5 && SEMC8 < MAC8)
            {
                if (C13_UBLB2_Dir == 1 && C21_UBLB1_Dir == 1)
                {
                    return CreateTrigger(context, 6.2999M, 6.2999M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.2949M, -6.2949M, basicTrigger);
                }
            }
            else if (SEMC5 > MAC5 && SEMC8 > MAC8)
            {
                return CreateTrigger(context, 6.2998M, 6.2998M, basicTrigger);
            }

            // else if(MAL2 < SEMC13 && MAL2 < SEMC21 
            // && MAL2 < MAC8 && MAL2 < MAC8 
            // && MAL2 < MAC13 && MAL2 < SEMC21 )
            // {
            //    return CreateTrigger(context, 6.2998M, 6.2998M, basicTrigger);
            // }
            else
            {
                return CreateTrigger(context, 6.2997M, 6.2997M, basicTrigger);
            }
        }

        if (basicTrigger == 6.28M)
        { // done
            return CreateTrigger(context, 6.2899M, 6.2899M, basicTrigger);
        }

        if (basicTrigger == 6.27M)
        { // Done

            // && MAC13 < MAC21 && SEMC21 < MAC21 && SEMC21 > LBC21
            // && SEMC13 < MAC13 && SEMC13 > LBC13
            // && SEMC8 < MAC8 && SEMC8 > LBC8
            // && SEMC13 < SEMC21 && SEMC8 < SEMC21
            // && MAL2 < MAC5 && MAL2 > LBC5) {   

            if (MAL2 >= SEMC5)
            {
                if (MAL2 > SEMC13 && MAL2 > SEMC8)
                {
                    return CreateTrigger(context, 6.2799M, 6.2799M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.2719M, 0, basicTrigger);
                }
            }
            else if (MAL2 < SEMC5)
            {
                if (MAL2 < SEMC13 && MAL2 < SEMC21
                && MAL2 < MAC1 && SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 6.2749M, -6.2749M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.2718M, 0, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.26M)
        { // done

            // && MAC13 > MAC21 && MAC21 > MAC34
            // && SEMC5 > MAC5 && SEMC8 > MAC8
            // && SEMC13 < MAC13 && SEMC21 > MAC21) {   

            if (SEMC3 < MAC3)
            {
                return CreateTrigger(context, 6.2699M, 6.2699M, basicTrigger);
            }
            else if (C3_SEMTrend != -0.1M && SEMC3 > MAC3)
            {
                return CreateTrigger(context, 6.2698M, 6.2698M, basicTrigger);
            }
            else if (SEMC3 > MAC3)
            {
                if (MAL2 > UBC13)
                {
                    return CreateTrigger(context, 6.2649M, -6.2649M, basicTrigger);
                }
                else if (MAL2 > SEMC8)
                {
                    if (MAL1 > UBC8 && MAL2 > UBC8 && MAL3 > UBC8)
                    {
                        return CreateTrigger(context, 6.2697M, 6.2697M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.2619M, 0, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 6.2648M, -6.2648M, basicTrigger);
                }
            }
        }


        if (basicTrigger == 7.87M)
        {
            if (SEMC1 < SEMC4)
            {
                if (SEMC1 < SEMC2 && SEMC1 < SEMC3)
                {
                    if (SEMC2 < MAC2)
                    {
                        return CreateTrigger(context, 7.8799M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.8798M, 0M, basicTrigger);
                    }
                }
                else if (SEMC1 < SEMC2)
                {
                    return CreateTrigger(context, 7.8797M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.8796M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 < SEMC3 && SEMC1 < SEMC2)
            {
                return CreateTrigger(context, 7.8795M, 0M, basicTrigger);
            }
            else if (SEMC1 < SEMC3)
            {
                return CreateTrigger(context, 7.8794M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.8791M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.85M)
        {
            if (LB2C1 > UBC3)
            {
                return CreateTrigger(context, 7.8599M, 0M, basicTrigger);
            }
            else if (LBC1 > UBH2)
            {
                return CreateTrigger(context, 7.8598M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.8519M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.81M)
        {
            if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == 1)
            {
                if (MAC1 > MAC5 && (C4_UBLB2_Dir == 1 || C5_UBLB1_Dir == 1))
                {
                    return CreateTrigger(context, 4.8199M, 0M, basicTrigger);
                }
                else if (MAL3 < LB2C2)
                {
                    return CreateTrigger(context, 4.8198M, 0M, basicTrigger);
                }
                else if (SEMC1 < LB2C2)
                {
                    if (LB2C4 < LB2C5)
                    {
                        return CreateTrigger(context, 4.8197M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.8196M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.8195M, 0M, basicTrigger);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.8194M, 0M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.8193M, 0M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 4.8192M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.8191M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.79M)
        {
            if (MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.7999M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.7998M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.97M)
        {
            if (SEMC3 < LB2C5)
            {
                return CreateTrigger(context, 4.9799M, 0M, basicTrigger);
            }
            else if (SEMC3 > SEMC5 && SEMC3 > LB2C8)
            {
                return CreateTrigger(context, 4.9798M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.9797M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.66M)
        {
            if (SEMC13 < SEMC21)
            {
                if (SEMC5 < MAC5 && SEMC5 < SEMC13)
                {
                    if (SEMC13 < LBC3)
                    {
                        return CreateTrigger(context, 4.6699M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.6698M, 0M, basicTrigger);
                    }
                }
                else if (MAC5 < LBC89 && SEMC89 < LBC89 && SEMC21 > SEMC89)
                {
                    if (SEMC5 > SEMC8)
                    {
                        return CreateTrigger(context, 4.6697M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.6696M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.6695M, 0M, basicTrigger);
                }
            }
            else if (SEMC21 > SEMC89 && SEMC89 < LBC89)
            {
                return CreateTrigger(context, 4.6694M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.6693M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.64M)
        {
            if (SEMC5 > SEMC8 && SEMC5 < MAC5 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.6499M, 0M, basicTrigger);
            }
            else if (SEMC5 > SEMC21 && MAC5 > SEMC21 && SEMC21 > MAC21 && SEMC1 < MAC1 && SEMC5 < UBC1 && MAC1 > LBC5)
            {
                return CreateTrigger(context, 4.6498M, 0M, basicTrigger);
            }
            else if (SEMC5 > SEMC8 && SEMC5 > MAC5 && SEMC2 > SEMC3 && SEMC3 > SEMC4 && MAL2 > SEMC5)
            {
                return CreateTrigger(context, 4.6497M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.6449M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.63M)
        {
            return CreateTrigger(context, 4.6399M, 0M, basicTrigger);
        }

        if (basicTrigger == 4.61M)
        {
            return CreateTrigger(context, 4.6199M, 0M, basicTrigger);
        }

        if (basicTrigger == 4.58M)
        {
            return CreateTrigger(context, 4.5899M, 0M, basicTrigger);
        }

        if (basicTrigger == 4.55M)
        {
            if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == -1)
            {
                if (SEMC13 < MAC3 && SEMC13 < LBC13 && MAC3 < LBC13)
                {
                    return CreateTrigger(context, 4.5599M, 0M, basicTrigger);
                }
                else if (C1_MAC_Pivot_Trend == 1)
                {
                    return CreateTrigger(context, 4.5598M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.5597M, 0M, basicTrigger);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == 1)
            {
                if (C1_MAC_Pivot_Trend == 1)
                {
                    if (MAC2 > MAC3)
                    {
                        return CreateTrigger(context, 4.5596M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.5595M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.5594M, 0M, basicTrigger);
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1 && C3_C13_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 4.5593M, 0M, basicTrigger);
            }
            else if (C5_C21_Flag_Trend_Lower == 1 && C3_C13_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.5592M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.18M)
        {
            if (C13_UBLB2_Dir == 1 || C21_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 4.1894M, 0M, basicTrigger);
            }
            else if (SEMC2 < MAC2 || SEMC3 < MAC3 || SEMC4 < MAC4 || SEMC5 < MAC5)
            {
                return CreateTrigger(context, 4.1889M, 0M, basicTrigger);
            }
            else if (MAC1 > UB2C21 && MAC2 > UB2C21)
            {
                return CreateTrigger(context, 4.1899M, 0M, basicTrigger);
            }
            else if (MAC1 > UB2C21 || MAC2 > UB2C21)
            {
                if (MAC2 > UB2C21)
                {
                    return CreateTrigger(context, 4.1898M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1897M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 < MAC2)
            {
                return CreateTrigger(context, 4.1891M, 0M, basicTrigger);
            }
            else if (LBC1 > UBC3)
            {
                return CreateTrigger(context, 4.1896M, 0M, basicTrigger);
            }
            else if (LBC1 > UBC4)
            {
                if (C13_UBLB2_Dir == 1 || C21_UBLB2_Dir == 1)
                {
                    return CreateTrigger(context, 4.1894M, 0M, basicTrigger);
                }
                else if (SEMC4 > SEMC5 && SEMC3 > SEMC4 && (SEML2 < LBC1 || SEML1 < LBC1 || SEMC1 < LBC1))
                {
                    return CreateTrigger(context, 4.1895M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1891M, 0M, basicTrigger);
                }
            }
            else if (LBC1 > UBC5)
            {
                if (SEMC1 < MAC2)
                {
                    return CreateTrigger(context, 4.1893M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1892M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.1818M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.26M)
        {
            if (SEMC13 < MAC13)
            {
                return CreateTrigger(context, 4.2699M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC13)
            {
                return CreateTrigger(context, 4.2698M, 0M, basicTrigger);
            }
            else if (MAL2 >= SEMC13)
            {
                return CreateTrigger(context, 4.2697M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.13M)
        {
            return CreateTrigger(context, 4.1397M, 0M, basicTrigger);
        }

        if (basicTrigger == 4.11M)
        {
            if (C21_UBLB2_Dir == 1)
            {
                if (SEMC5 > UBC5)
                {
                    if (MAL2 > UBC5)
                    {
                        if ((SEMC1 < SEMC5 || SEMC2 < SEMC5 || SEMC3 < SEMC5 || SEMC4 < SEMC5) && (MAL2 > SEMC5 || MAL2 > SEMC4) && SEMC2 > SEMC3 && SEMC3 > SEMC4)
                        {
                            return CreateTrigger(context, 4.1199M, 0M, basicTrigger);
                        }
                        else if (SEMC1 > SEMC5 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && MAL2 < SEMC1)
                        {
                            return CreateTrigger(context, 4.1149M, 0M, basicTrigger);
                        }
                        else if (MAL2 < SEMC5 && MAL2 < SEMC4 && MAL2 < SEMC1 && MAL2 < SEMC13)
                        {
                            return CreateTrigger(context, 4.1148M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.1119M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 <= UBC5)
                    {
                        if (MAC1 < UBC5 && SEMC2 < UBC5 && SEMC3 < UBC5 && SEMC4 < UBC5 && SEMC1 < MAC1)
                        {
                            return CreateTrigger(context, 4.1198M, 0M, basicTrigger);
                        }
                        else if (MAC1 < UBC5 && (SEMC1 > UBC5 || SEMC2 > UBC5 || SEMC3 > UBC5 || SEMC4 > UBC5) && (SEMC2 < UBC5 || SEMC3 < UBC5 || SEMC4 < UBC5) && SEMC2 > LBC2 && MAL2 > LBC2 && MAL2 > MAC3)
                        {
                            return CreateTrigger(context, 4.1197M, 0M, basicTrigger);
                        }
                        else if (MAC1 < UBC5 && SEMC2 > UBC5 && SEMC3 > UBC5 && SEMC4 > UBC5 && MAL2 > SEMC1 && SEMC1 < MAC1 && MAL2 > MAC2 && MAL2 > MAC3)
                        {
                            return CreateTrigger(context, 4.1196M, 0M, basicTrigger);
                        }
                        else if (MAC1 > UBC5)
                        {
                            return CreateTrigger(context, 4.1147M, 0M, basicTrigger);
                        }
                        else if (MAC1 < UBC5 && (SEMC1 > UBC5 || SEMC2 > UBC5 || SEMC3 > UBC5 || SEMC4 > UBC5) && MAL2 < LBC2)
                        {
                            return CreateTrigger(context, 4.1146M, 0M, basicTrigger);
                        }
                        else if (MAC1 < UBC5 && (SEMC2 > UBC5 || SEMC3 > UBC5 || SEMC4 > UBC5) && (SEMC2 < UBC5 || SEMC3 < UBC5 || SEMC4 < UBC5) && MAL2 < SEMC2)
                        {
                            return CreateTrigger(context, 4.1145M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.1118M, 0M, basicTrigger);
                        }
                    }
                }
                else if (SEMC5 < MAC5)
                {
                    if (SEMC1 > MAC1)
                    {
                        return CreateTrigger(context, 4.1195M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.1117M, 0M, basicTrigger);
                    }
                }
                else if (SEMC5 >= MAC5 && SEMC5 <= UBC5)
                {
                    if (SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC4 > MAC4)
                    {
                        return CreateTrigger(context, 4.1194M, 0M, basicTrigger);
                    }
                    else if (SEMC1 < MAC1 && SEMC2 < MAC2 && SEMC3 < MAC3 && SEMC4 < MAC4 && SEMC5 > MAC5 && MAL2 > MAC8)
                    {
                        return CreateTrigger(context, 4.1191M, 0M, basicTrigger);
                    }
                    else if (SEMC3 < MAC3 && SEMC4 < MAC4 && SEMC2 > MAC2 && MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 4.1144M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 4.1189M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 4.1115M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 4.12M)
        {
            if (UBC13 < MAC21)
            {
                return CreateTrigger(context, 4.1298M, 0M, basicTrigger);
            }
            else if (UB2C13 < UBC21)
            {
                return CreateTrigger(context, 4.1297M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.09M)
        {
            if (SEMC5 >= MAC5 && SEMC5 < UBC5)
            {
                return CreateTrigger(context, 4.0999M, 0M, basicTrigger);
            }
            else if (SEMC5 > UBC5)
            {
                return CreateTrigger(context, 4.0998M, 0M, basicTrigger);
            }
            else if (SEMC5 < MAC5)
            {
                return CreateTrigger(context, 4.0997M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0996M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.79M)
        {
            if (MAC2 > UBC5 && MAC1 > UBC5)
            {
                return CreateTrigger(context, 7.7999M, 0M, basicTrigger);
            }
            else if (MAC2 < UBC5 && MAC1 > UBC5)
            {
                return CreateTrigger(context, 7.7998M, 0M, basicTrigger);
            }
            else if (MAC2 > UBC5 && MAC1 < UBC5)
            {
                if (SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 7.7992M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7991M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.7996M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.78M)
        {
            if (SEMC1 > UB2C5 || SEMC2 > UB2C5 || SEMC3 > UB2C5)
            {
                if (SEMC5 > Math.Max(UB2C13, UB2C21) && SEMC5 > UBC5)
                {
                    return CreateTrigger(context, 7.7899M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7898M, 0M, basicTrigger);
                }
            }
            else if (SEMC8 < UBC8 && SEMC13 < UBC13 && SEMC21 < UBC21)
            {
                return CreateTrigger(context, 7.7897M, 0M, basicTrigger);
            }
            else if (SEMC8 < UBC8 && SEMC13 < UBC13)
            {
                return CreateTrigger(context, 7.7896M, 0M, basicTrigger);
            }
            else if (SEMC8 < UBC8)
            {
                return CreateTrigger(context, 7.7895M, 0M, basicTrigger);
            }
            else if (SEMC13 < UBC13)
            {
                return CreateTrigger(context, 7.7894M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.7893M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.75M)
        {
            if (MAL2 > SEMC21)
            {
                if (C5_SEMTrend != -0.1M)
                {
                    if (SEMC5 <= UBC5)
                    {
                        if (SEMC2 > SEMC3)
                        {
                            return CreateTrigger(context, 7.7599M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7519M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 > UBC5)
                    {
                        if (SEMC3 > SEMC4)
                        {
                            return CreateTrigger(context, 7.7598M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.7518M, 0M, basicTrigger);
                        }
                    }
                }
                else if (C5_SEMTrend == -0.1M)
                {
                    return CreateTrigger(context, 7.7597M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 > UBC13)
            {
                if (SEMC5 > UBC5 && MAC1 > UBC5 && MAL2 > UBC5)
                {
                    return CreateTrigger(context, 7.7596M, 0M, basicTrigger);
                }
                else if (SEMC5 < UBC5 || MAC1 < UBC5 || MAL2 < UBC5)
                {
                    if (SEMC2 < MAC2 && SEMC3 > MAC3 && MAC1 > MAC5)
                    {
                        return CreateTrigger(context, 7.7595M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7594M, 0M, basicTrigger);
                    }
                }
            }
            else if (SEMC8 > SEMC13)
            {
                if (MAL2 > SEMC8 || MAL2 > SEMC13)
                {
                    if (SEMC1 < MAC1)
                    {
                        return CreateTrigger(context, 7.7593M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7592M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.7591M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5)
            {
                if (MAL2 > UBC5 && MAC1 > UBC5)
                {
                    return CreateTrigger(context, 7.7587M, 0M, basicTrigger);
                }
                else if (MAL2 < UBC5 && MAC1 < UBC5)
                {
                    if (MAL2 < SEMC2)
                    {
                        return CreateTrigger(context, 7.7549M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.7548M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 7.7516M, 0M, basicTrigger);
                }
            }
            else if (SEMC13 < UBC13 && SEMC5 < UBC5)
            {
                if (MAL2 < MAC21)
                {
                    return CreateTrigger(context, 7.7584M, 0M, basicTrigger);
                }
                else if (MAL2 < MAC13)
                {
                    return CreateTrigger(context, 7.7581M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.7577M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.73M)
        {
            if (C5_C13_S_Cont_MAC < 0 && cParam.C3UB2_C3MAC == -1)
            {
                return CreateTrigger(context, 7.7399M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.7396M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.39M)
        {
            if (SEMC21 < LBC21 && SEMC13 < LBC13 && MAC21 > MAC89 && SEMC34 < LBC34)
            {
                if (SEMC13 > SEMC21)
                {
                    return CreateTrigger(context, 7.3999M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.3949M, 0M, basicTrigger);
                }
            }
            else if (SEMC21 < LBC21 && SEMC13 > LBC13 && SEMC13 < MAC13 && SEMC13 > SEMC21)
            {
                return CreateTrigger(context, 7.3998M, 0M, basicTrigger);
            }
            else if (SEMC55 > MAC55 && SEMC89 > MAC89)
            {
                return CreateTrigger(context, 7.3997M, 0M, basicTrigger);
            }
            else if (SEMC55 < MAC55 && SEMC89 > MAC89)
            {
                return CreateTrigger(context, 7.3996M, 0M, basicTrigger);
            }
            else if (SEMC55 > MAC55 && SEMC89 < MAC89)
            {
                return CreateTrigger(context, 7.3995M, 0M, basicTrigger);
            }
            else if (SEMC55 < MAC55 && SEMC89 < MAC89)
            {
                return CreateTrigger(context, 7.3994M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.3993M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.38M)
        {
            if (SEMC13 > SEMC21)
            {
                return CreateTrigger(context, 7.3849M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.3898M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.37M)
        {
            if (MAC1 > UBC13)
            {
                return CreateTrigger(context, 7.3799M, 0M, basicTrigger);
            }
            else if (MAC1 > MAC21)
            {
                return CreateTrigger(context, 7.3792M, 0M, basicTrigger);
            }
            else if (SEMC8 > UBC8 && SEMC5 > UBC5 && (MAC1 > UBC8 || MAC1 > UBC5 || MAC1 > MAC3))
            {
                return CreateTrigger(context, 7.3798M, 0M, basicTrigger);
            }
            else if (SEMC8 > MAC8 && SEMC5 > MAC5 && SEMC8 < UBC8 && SEMC5 < UBC5 && C5_SEMTrend != -0.1M && C8_SEMTrend != -0.1M && MAL2 > SEMC5 && MAL2 > SEMC8)
            {
                return CreateTrigger(context, 7.3797M, 0M, basicTrigger);
            }
            else if (SEMC8 > MAC8)
            {
                if (MAL2 >= SEMC8)
                {
                    return CreateTrigger(context, 7.3796M, 0M, basicTrigger);
                }
                else if (MAL2 < SEMC8)
                {
                    if (MAL2 > SEMC3 && MAL2 > SEMC5)
                    {
                        return CreateTrigger(context, 7.3795M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.3749M, 0M, basicTrigger);
                    }
                }
            }
            else if (MAL2 < MAC1 && MAL1 < MAC1 && ((MAL2 < SEMC3 && MAL2 < SEMC5) || (MAL2 < SEMC2 && MAL2 < SEMC3)))
            {
                return CreateTrigger(context, 7.3748M, 0M, basicTrigger);
            }
            else if (MAC21 < MAC89)
            {
                if (SEMC8 > MAC8 && MAL2 > LBC5 && MAL2 > SEMC13)
                {
                    return CreateTrigger(context, 7.3793M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.3747M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.31M)
        {
            if (MAL2 < MAC21 && MAL2 > SEMC21)
            {
                return CreateTrigger(context, 7.3199M, 0M, basicTrigger);
            }
            else if (MAL2 > UBC21)
            {
                return CreateTrigger(context, 7.3198M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC21 && MAL2 < MAC21 && MAL2 < LBC5)
            {
                return CreateTrigger(context, 7.3197M, 0M, basicTrigger);
            }
            else if (MAL2 < LBC21)
            {
                return CreateTrigger(context, 7.3196M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.3195M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.33M)
        {
            if (MAL2 < SEMC13)
            {
                return CreateTrigger(context, 7.3349M, 0M, basicTrigger);
            }
            else if (SEMC21 < UBC21)
            {
                if (SEMC13 > MAC3)
                {
                    return CreateTrigger(context, 7.3399M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.3398M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > UB2C21)
            {
                if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 7.3397M, 0M, basicTrigger);
                }
                else if ((SEMC2 < MAC2) || (SEMC2 < MAC2 && C2_SEMTrend != 0))
                {
                    return CreateTrigger(context, 7.3396M, 0M, basicTrigger);
                }
                else if (MAL2 > SEMC8 && SEMC5 > SEMC8 && SEMC3 > SEMC8 && SEMC2 > SEMC8 && SEMC1 > SEMC8)
                {
                    return CreateTrigger(context, 7.3395M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.3394M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC3)
            {
                return CreateTrigger(context, 7.3393M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.3386M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.27M)
        {
            return CreateTrigger(context, 7.2796M, 0M, basicTrigger);
        }

        if (basicTrigger == 7.09M)
        {
            if (SEMC13 > MAC13 && SEMC21 > LBC21 && SEMC21 < UBC21 && (SEMC21 < SEMC89 || SEMC21 < SEMC55) && MAC21 > MAC89 && SEMC13 > SEMC21 && SEMC89 < UBC21 && SEMC55 < UBC21 && SEMC34 < UBC21 && SEMC89 > MAC21 && SEMC55 > MAC21)
            {
                return CreateTrigger(context, 7.0991M, 0M, basicTrigger);
            }
            else if (MAL2 < LB2C5)
            {
                return CreateTrigger(context, 7.0919M, 0M, basicTrigger);
            }
            else if (C13_SEMTrend == -0.1M)
            {
                if (MAL2 > UBC13)
                {
                    return CreateTrigger(context, 7.0999M, 0M, basicTrigger);
                }
                else if (MAL2 > UBC21)
                {
                    return CreateTrigger(context, 7.0998M, 0M, basicTrigger);
                }
                else if (MAL2 > MAC13)
                {
                    if (SEMC5 > MAC5)
                    {
                        if (MAL2 > SEMC5 && SEMC5 < UBC5)
                        {
                            return CreateTrigger(context, 7.0997M, 0M, basicTrigger);
                        }
                        else if (MAL2 < MAC5 && MAL2 < SEMC5 && MAL2 < MAC1)
                        {
                            return CreateTrigger(context, 7.0949M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.0919M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 <= MAC5)
                    {
                        if (SEMC1 > MAC1 && MAL2 < MAC5 && MAL2 < MAC8)
                        {
                            return CreateTrigger(context, 7.0948M, 0M, basicTrigger);
                        }
                        else if (SEMC5 < LBC5)
                        {
                            return CreateTrigger(context, 7.0996M, 0M, basicTrigger);
                        }
                        else if (SEMC4 < LBC4)
                        {
                            return CreateTrigger(context, 7.0995M, 0M, basicTrigger);
                        }
                        else if (SEMC3 < LBC3)
                        {
                            return CreateTrigger(context, 7.0994M, 0M, basicTrigger);
                        }
                        else if (SEMC2 < LBC2)
                        {
                            return CreateTrigger(context, 7.0993M, 0M, basicTrigger);
                        }
                        else if (MAL2 < MAC5 && MAL2 < MAC8)
                        {
                            return CreateTrigger(context, 7.0947M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.0918M, 0M, basicTrigger);
                        }
                    }
                }
                else if (MAL2 <= MAC13)
                {
                    if (SEMC5 < LBC5)
                    {
                        if (SEMC8 >= MAC8)
                        {
                            return CreateTrigger(context, 7.0992M, 0M, basicTrigger);
                        }
                        else if (SEMC8 < MAC8)
                        {
                            return CreateTrigger(context, 7.0946M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 7.0917M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < MAC5 && MAL2 < MAC8 && MAL2 < SEMC5)
                    {
                        return CreateTrigger(context, 7.0945M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0916M, 0M, basicTrigger);
                    }
                }
            }
            else if (C13_SEMTrend != -0.1M)
            {
                if (C5_SEMTrend == -0.1M)
                {
                    return CreateTrigger(context, 7.0944M, 0M, basicTrigger);
                }
                else if (C3_SEMTrend == -0.1M)
                {
                    return CreateTrigger(context, 7.0989M, 0M, basicTrigger);
                }
                else if (C2_SEMTrend == -0.1M)
                {
                    return CreateTrigger(context, 7.0988M, 0M, basicTrigger);
                }
                else if (MAL2 > MAC13)
                {
                    if (SEMC2 > MAC2 && SEMC1 > MAC1)
                    {
                        return CreateTrigger(context, 7.0943M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 7.0986M, 0M, basicTrigger);
                    }
                }
                else if (MAL2 <= MAC13)
                {
                    return CreateTrigger(context, 7.0985M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 7.72M)
        {
            return CreateTrigger(context, 7.7296M, 0M, basicTrigger);
        }

        if (basicTrigger == 7.08M)
        {
            return CreateTrigger(context, 7.0899M, 0M, basicTrigger);
        }

        if (basicTrigger == 7.02M)
        {
            if (MAL2 > MAC1 && MAL1 > MAC1 && SEMC1 > MAC1 && SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 7.0299M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.0246M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.03M)
        {
            return CreateTrigger(context, 7.0399M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.78M)
        {
            return CreateTrigger(context, 6.7899M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.71M)
        {
            return CreateTrigger(context, 6.7199M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.69M)
        {
            if (SEMC5 < MAC5)
            {
                if (SEMC5 < LBC5 && SEMC3 < LB2C5)
                {
                    return CreateTrigger(context, 6.6999M, 0M, basicTrigger);
                }
                else if (SEML2 > SEMC5 || SEML1 > SEMC5 || SEMC1 > SEMC5)
                {
                    return CreateTrigger(context, 6.6997M, 0M, basicTrigger);
                }
                else if (SEML1 < SEMC1 || SEML2 < SEMC1)
                {
                    return CreateTrigger(context, 6.6948M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.6918M, 0M, basicTrigger);
                }
            }
        }

        if (basicTrigger == 6.68M)
        {
            return CreateTrigger(context, 6.6899M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.67M)
        {
            return CreateTrigger(context, 6.6799M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.63M)
        {
            return CreateTrigger(context, 6.6399M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.62M)
        {
            return CreateTrigger(context, 6.6299M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.61M)
        {
            return CreateTrigger(context, 6.6199M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.59M)
        {
            return CreateTrigger(context, 6.5999M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.58M)
        {
            return CreateTrigger(context, 6.5899M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.57M)
        {
            if (MAC8 < LBC21 || MAC5 < LBC21)
            {
                if (MAC3 < LBC21)
                {
                    return CreateTrigger(context, 6.5749M, 0M, basicTrigger);
                }
                else if (MAC3 >= LBC21)
                {
                    return CreateTrigger(context, 6.5799M, 0M, basicTrigger);
                }
            }
            else if (MAC8 < LBC21 || MAC5 < LBC21)
            {
                return CreateTrigger(context, 6.5798M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC2)
            {
                if (SEMC3 < MAC5 && SEMC3 < SEMC5)
                {
                    return CreateTrigger(context, 6.5797M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.5796M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 > MAC5 && SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 6.5795M, 0M, basicTrigger);
            }
            else if (SEMC3 > MAC5 || SEMC3 > SEMC5)
            {
                return CreateTrigger(context, 6.5794M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.5793M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.55M)
        {
            if (LB2C1 > MAC2)
            {
                return CreateTrigger(context, 6.5599M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.5519M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.54M)
        {
            if (MAL2 > UB2C13 && MAL2 > UB2C21)
            {
                return CreateTrigger(context, 6.5499M, 0M, basicTrigger);
            }
            else if (SEMC2 > SEMC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5 && SEMC5 > SEMC8)
            {
                return CreateTrigger(context, 6.5498M, 0M, basicTrigger);
            }
            else if (SEMC2 > SEMC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5 && SEMC5 < SEMC8)
            {
                return CreateTrigger(context, 6.5497M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.5496M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.39M)
        {
            if (MAC1 > UB2C8)
            {
                return CreateTrigger(context, 6.3949M, 0M, basicTrigger);
            }
            else if (LB2C1 > MAC2)
            {
                return CreateTrigger(context, 6.3998M, 0M, basicTrigger);
            }
            else if (MAL2 > UB2C8)
            {
                return CreateTrigger(context, 6.3997M, 0M, basicTrigger);
            }
            else if (MAL1 < SEMC5)
            {
                return CreateTrigger(context, 6.3996M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.3995M, 0M, basicTrigger);
            }
        }


        if (basicTrigger == 6.34M)
        {
            // && SEMC21 < MAC21 
            // && SEMC89 > MAC89 && MAC21 < MAC89
            // && MAL2 <= MAC89) {   

            if (SEMC13 > MAC13)
            {
                return CreateTrigger(context, 6.3499M, 0M, basicTrigger);
            }
            else if (MAC21 < LBC89)
            {
                return CreateTrigger(context, 6.3498M, 0M, basicTrigger);
            }
            else if (SEMC21 < LBC89)
            {
                return CreateTrigger(context, 6.3497M, 0M, basicTrigger);
            }
            else if (SEMC21 < LBC13 && SEMC13 < LBC13)
            {
                return CreateTrigger(context, 6.3496M, 0M, basicTrigger);
            }
            else if (SEMC21 < LBC13 && SEMC13 > LBC13)
            {
                return CreateTrigger(context, 6.3495M, 0M, basicTrigger);
            }
            else if (SEMC21 > LBC13 && SEMC21 < LBC21)
            {
                return CreateTrigger(context, 6.3494M, 0M, basicTrigger);
            }
            else if (SEMC21 > LBC21 && SEMC13 > LBC13)
            {
                return CreateTrigger(context, 6.3493M, 0M, basicTrigger);
            }
            else if (SEMC21 > LBC21 && SEMC13 < LBC13)
            {
                return CreateTrigger(context, 6.3449M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 6.3492M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.33M)
        {
            return CreateTrigger(context, 6.3397M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.32M)
        {
            // && SEMC21 > MAC21 
            // && (SEMC89 < MAC89 || MAC21 < MAC89)
            // && MAL2 <= MAC89) {   

            if (C5_SEMTrend == -0.1M)
            {
                return CreateTrigger(context, 6.3299M, 0M, basicTrigger);
            }
            else if (C5_SEMTrend != -0.1M && SEMC5 > MAC5)
            {
                return CreateTrigger(context, 6.3298M, 0M, basicTrigger);
            }
            else if (SEMC5 < MAC5)
            {
                return CreateTrigger(context, 6.3297M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.31M)
        {
            if (SEMC2 <= MAC2)
            {
                return CreateTrigger(context, 6.3199M, 0M, basicTrigger);
            }
            else if (SEMC2 > MAC2)
            {
                return CreateTrigger(context, 6.3197M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.25M)
        {
            // && MAC13 > MAC21 && MAC8 > MAC21
            // && SEMC5 > MAC5 && SEMC8 < MAC8
            // && SEMC13 > MAC13 && SEMC21 > MAC21) {   

            if ((MAL2 > SEMC8 || MAL1 > SEMC8)
                && MAL2 < UBC5 && MAL2 < UBC8 && MAL2 < UBC13)
            {
                if (MAL2 < MAC2)
                {
                    if (SEMC5 < MAC8)
                    {
                        return CreateTrigger(context, 6.2599M, 0M, basicTrigger);
                    }
                    else if (MAL2 > LBC3 && SEMC2 > LBC3)
                    {
                        return CreateTrigger(context, 6.2598M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 6.2519M, 0M, basicTrigger);
                    }
                }
                else if (C3_SEMTrend != -0.1M && SEMC3 > MAC3)
                {
                    return CreateTrigger(context, 6.2597M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.2596M, 0M, basicTrigger);
                }
            }
            else if (MAL2 < MAC5 && MAL2 < MAC8 && MAL2 < MAC13 && MAL2 < MAC21)
            {
                return CreateTrigger(context, 6.2549M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC8 && MAL1 < SEMC8)
            {
                if (SEMC3 > LBC3)
                {
                    return CreateTrigger(context, 6.2548M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 6.2518M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > UBC5 && MAL2 > UBC8 && MAL2 > UBC13)
            {
                return CreateTrigger(context, 6.2595M, 0M, basicTrigger);
            }
            else if (MAL2 > UBC5 && SEMC5 < UBC5
                && MAL2 < SEMC21 && SEMC21 > UBC21 && SEMC21 > UBC5)
            {
                return CreateTrigger(context, 6.2547M, 0M, basicTrigger);
            }
            else if (MAL2 > UBC5 && SEMC5 > UBC5
                && MAL2 < SEMC13 && SEMC13 > UBC13
                && MAL2 < SEMC5 && SEMC21 < UBC21)
            {
                return CreateTrigger(context, 6.2546M, 0M, basicTrigger);
            }
            // else if(SEMC5 < MAC8 || SEMC13 < MAC8)
            // {
            //  return CreateTrigger(context, 6.2548M, 6.0M, basicTrigger);
            // }
            else
            {
                return CreateTrigger(context, 6.2517M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 6.24M)
        {
            return CreateTrigger(context, 6.2498M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.23M)
        {
            return CreateTrigger(context, 6.2398M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.22M)
        {
            return CreateTrigger(context, 6.2298M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.21M)
        {
            return CreateTrigger(context, 6.2198M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.53M)
        {
            return CreateTrigger(context, 6.5397M, 0M, basicTrigger);
        }

        if (basicTrigger == 6.49M)
        {
            return CreateTrigger(context, 6.4997M, 0M, basicTrigger);
        }

        if (basicTrigger == 7.07M)
        {
            if (MAC3 < MAC13)
            {
                return CreateTrigger(context, 7.0799M, 0M, basicTrigger);
            }
            else if (C3_SEMTrend == -0.1M)
            {
                return CreateTrigger(context, 7.0798M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.0796M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.44M)
        {
            return CreateTrigger(context, 4.4499M, 0M, basicTrigger);
        }

        if (basicTrigger == 7.71M)
        {
            if (MAL2 > SEMC8)
            {
                return CreateTrigger(context, 7.7199M, 0M, basicTrigger);
            }
            else if (MAL2 <= SEMC8)
            {
                return CreateTrigger(context, 7.7196M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.56M)
        {
            if (MAL2 > MAC13 && MAL2 > MAC21)
            {
                return CreateTrigger(context, 7.5699M, 0M, basicTrigger);
            }
            else if (MAL2 > MAC13)
            {
                return CreateTrigger(context, 7.5698M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.5697M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.68M)
        {
            if (SEMC1 > UB2C3)
            {
                return CreateTrigger(context, 7.6897M, 0M, basicTrigger);
            }
            else if (SEMC2 > UB2C3)
            {
                return CreateTrigger(context, 7.6896M, 0M, basicTrigger);
            }
            else if (MAL2 > UB2C5)
            {
                return CreateTrigger(context, 7.6895M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6894M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.67M)
        {
            if (SEMC1 < MAC1)
            {
                if (SEMC5 > UBC5 && SEMC8 > UBC8 && SEMC5 > SEMC8)
                {
                    return CreateTrigger(context, 7.6799M, 0M, basicTrigger);
                }
                else if (SEMC2 > UB2C4)
                {
                    return CreateTrigger(context, 7.6798M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6797M, 0M, basicTrigger);
                }
            }
            else if (SEMC2 > UB2C3)
            {
                return CreateTrigger(context, 7.6796M, 0M, basicTrigger);
            }
            else if (SEMC2 > UB2C4)
            {
                if (SEMC1 > UBC1)
                {
                    return CreateTrigger(context, 7.6795M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6794M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 > UBC1)
            {
                if (SEMC5 > UBC5 && SEMC2 < UB2C4)
                {
                    return CreateTrigger(context, 7.6793M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6792M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > UBC5)
            {
                return CreateTrigger(context, 7.6791M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6789M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.66M)
        {
            if (SEMC2 < SEMC3)
            {
                if (SEMC1 > MAC1 && pC1_UBLB1_Dir == -1 && MAC1 > SEMC5)
                {
                    return CreateTrigger(context, 7.6699M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 7.6698M, 0M, basicTrigger);
                }
            }
            else if (SEMC1 < UBC1 && SEMC5 < UBC5)
            {
                return CreateTrigger(context, 7.6696M, 0M, basicTrigger);
            }
            else if (SEMC1 < MAC1)
            {
                return CreateTrigger(context, 7.6695M, 0M, basicTrigger);
            }
            else if (MAL2 > SEMC4)
            {
                if (pC1_UBLB1_Dir == -1)
                {
                    return CreateTrigger(context, 7.6694M, 0M, basicTrigger);
                }

                if (pC1_UBLB1_Dir == -1)
                {
                    return CreateTrigger(context, 7.6693M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 7.6692M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 7.64M)
        {
            if (C21_UBLB2_Dir == 1)
            {
                return CreateTrigger(context, 7.6499M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 7.6498M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.04M)
        {
            if (SEMC2 < LB2C4)
            {
                return CreateTrigger(context, 4.0499M, 0M, basicTrigger);
            }
            else if (MAC1 > MAC5)
            {
                return CreateTrigger(context, 4.0492M, 0M, basicTrigger);
            }
            else if (MAC4 > MAC5 || MAC2 > MAC5 || MAC3 > MAC5)
            {
                if (SEMC13 > SEMC21)
                {
                    return CreateTrigger(context, 4.0498M, 0M, basicTrigger);
                }
                else if (SEMC13 <= SEMC21)
                {
                    if (SEMC13 > UBC13 && SEMC21 > UBC21 && MAL2 < SEMC5 && SEMC3 < SEMC4 && SEMC3 < SEMC5 && SEMC2 < SEMC4 && SEMC2 < SEMC5)
                    {
                        return CreateTrigger(context, 4.0497M, 0M, basicTrigger);
                    }
                    else if (SEMC13 < UBC13 && MAC8 < MAC13)
                    {
                        if (MAL2 > SEMC13 && SEMC1 > SEMC13 && SEMC2 > SEMC13 && SEMC3 > SEMC13)
                        {
                            return CreateTrigger(context, 4.0496M, 0M, basicTrigger);
                        }
                        else if (SEMC1 < UBC8 && SEMC2 < UBC8 && SEMC3 < UBC8 && SEMC4 < UBC8 && SEMC5 < UBC8 && SEMC13 < UBC8 && SEMC21 < UBC8)
                        {
                            return CreateTrigger(context, 4.0495M, 0M, basicTrigger);
                        }
                        else if (MAL2 < SEMC13 && SEMC1 < SEMC13 && SEMC2 < SEMC13 && SEMC3 < SEMC13)
                        {
                            return CreateTrigger(context, 4.0495M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 4.0494M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 4.0493M, 0M, basicTrigger);
                    }
                }
            }
            else if (C21_UBLB2_Dir == 1 && SEMC13 > UBC13)
            {
                return CreateTrigger(context, 4.0492M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.0491M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.24M)
        {
            if (SEMC3 > UB2C13 || SEMC2 > UB2C13 || SEMC4 > UB2C13 || SEMC5 > UB2C13)
            {
                return CreateTrigger(context, 4.2499M, 0M, basicTrigger);
            }
            else if (MAL2 > UB2C21)
            {
                return CreateTrigger(context, 4.2498M, 0M, basicTrigger);
            }
            else if (SEMC3 > UB2C21 && SEMC2 > UB2C21 && SEMC4 > UB2C21 && SEMC5 > UB2C21)
            {
                return CreateTrigger(context, 4.2497M, 0M, basicTrigger);
            }
            else if (MAL2 < SEMC13 && MAL2 < UB2C21)
            {
                if (MAL2 < SEMC21)
                {
                    return CreateTrigger(context, 4.2496M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.2495M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.2494M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.23M)
        {
            if (MAC5 < MAC89)
            {
                return CreateTrigger(context, 4.2399M, 0M, basicTrigger);
            }
            else if (C21_Pivot_Status < 0)
            {
                return CreateTrigger(context, 4.2398M, 0M, basicTrigger);
            }
            else if (C13_Pivot_Status < 0)
            {
                return CreateTrigger(context, 4.2397M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.2396M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.21M)
        {
            if (UB2C13 < UB2C21)
            {
                return CreateTrigger(context, 4.2199M, 0M, basicTrigger);
            }
            else if (SEMC13 < SEMC21)
            {
                return CreateTrigger(context, 4.2198M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.2194M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.16M)
        {
            if (C3_C13_Flag_Trend_Lower == -1 && C5_C21_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.1699M, 0M, basicTrigger);
            }
            else if (C3_C13_Flag_Trend_Lower == 1 && C5_C21_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 4.1698M, 0M, basicTrigger);
            }
            else if (C3_C13_Flag_Trend_Lower == -1 && C5_C21_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 4.1697M, 0M, basicTrigger);
            }
            else if (C3_C13_Flag_Trend_Lower == 1 && C5_C21_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 4.1696M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.15M)
        {
            if (MAC1 > UB2C21 && MAC2 < UB2C21)
            {
                if (SEMC1 > MAC1)
                {
                    return CreateTrigger(context, 4.1599M, 0M, basicTrigger);
                }
                else if (SEMC1 <= MAC1)
                {
                    return CreateTrigger(context, 4.1598M, 0M, basicTrigger);
                }
            }
            else if (MAC1 < UB2C21 && MAC2 > UB2C21)
            {
                return CreateTrigger(context, 4.1595M, 0M, basicTrigger);
            }
            else if (MAC1 > UB2C21 && MAC2 > UB2C21)
            {
                if (LBC2 > UB2C21)
                {
                    return CreateTrigger(context, 4.1594M, 0M, basicTrigger);
                }
                else if (SEMC2 < MAC2)
                {
                    return CreateTrigger(context, 4.1593M, 0M, basicTrigger);
                }
                else if (SEMC1 < MAC1)
                {
                    return CreateTrigger(context, 4.1592M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1591M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.1589M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.17M)
        {
            if (C3_C8_S_Cont_LB1 == -1 && C3_C5_S_Cont_LB1 == -1)
            {
                return CreateTrigger(context, 4.1792M, 0M, basicTrigger);
            }
            else if (MAL2 > UB2C13)
            {
                return CreateTrigger(context, 4.1799M, 0M, basicTrigger);
            }
            else if (SEMC1 > MAC1 && SEMC2 > MAC2 && SEMC3 > MAC3)
            {
                if (C2_SEMTrend != -0.1M || C3_SEMTrend != -0.1M)
                {
                    return CreateTrigger(context, 4.1798M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.1797M, 0M, basicTrigger);
                }
            }
            else if (SEMC3 < MAC3)
            {
                return CreateTrigger(context, 4.1796M, 0M, basicTrigger);
            }
            else if (SEMC2 < MAC2)
            {
                return CreateTrigger(context, 4.1795M, 0M, basicTrigger);
            }
            else if (SEMC1 < MAC1)
            {
                return CreateTrigger(context, 4.1794M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.1793M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.22M)
        {
            if (SEMC13 > UBC13)
            {
                return CreateTrigger(context, 4.2299M, 0M, basicTrigger);
            }
            else if (SEMC8 > UBC8)
            {
                return CreateTrigger(context, 4.2298M, 0M, basicTrigger);
            }
            else if (SEMC5 > UBC5)
            {
                return CreateTrigger(context, 4.2297M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.2296M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.95M)
        {
            return CreateTrigger(context, 4.9599M, 0M, basicTrigger);
        }

        if (basicTrigger == 4.46M)
        {
            if (SEMC21 > MAC89)
            {
                if (MAL2 > SEMC21)
                {
                    return CreateTrigger(context, 4.4699M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4698M, 0M, basicTrigger);
                }
            }
            else if (SEMC5 > SEMC21 && SEMC8 > SEMC21)
            {
                if (SEMC5 < MAC5 && SEMC1 > SEMC5)
                {
                    return CreateTrigger(context, 4.4697M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4696M, 0M, basicTrigger);
                }
            }
            else if (MAL2 > MAC89)
            {
                if (SEMC1 > SEMC2)
                {
                    return CreateTrigger(context, 4.4695M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 4.4694M, 0M, basicTrigger);
                }
            }
            else
            {
                return CreateTrigger(context, 4.4693M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 1.93M)
        {
            if (C5_SEMShortTrend == -1)
            {
                return CreateTrigger(context, 1.9349M, 0M, basicTrigger);
            }
            else if (MAL2 < MAC5 && C5_SEMTrend != -0.1M && SEMC2 > MAC2 && MAL2 > MAC2 && SEMC1 > MAC1 && MAL2 > MAC1 && MAC1 > SEMC5 && SEMC1 > SEMC5 && MAC1 > SEMC4 && SEMC1 > SEMC4 && MAC2 > SEMC5 && SEMC2 > SEMC5 && MAC2 > SEMC4 && SEMC2 > SEMC4)
            {
                return CreateTrigger(context, 1.9397M, 0M, basicTrigger);
            }
            else if (MAL2 >= MAC5 && C5_SEMTrend != -0.1M && SEMC1 > MAC1 && MAL2 > MAC1 && MAC1 > SEMC5 && SEMC1 > SEMC5 && MAC1 > SEMC4 && SEMC1 > SEMC4)
            {
                return CreateTrigger(context, 1.9395M, 0M, basicTrigger);
            }
            else if (MAL2 < MAC5 && SEMC1 > MAC1 && MAL2 > MAC1 && MAC1 > SEMC5 && SEMC1 > SEMC5 && MAC1 > SEMC4 && SEMC1 > SEMC4)
            {
                return CreateTrigger(context, 1.9319M, 0M, basicTrigger);
            }
            else if (SEMC5 < MAC1 && SEMC1 < SEMC5 && SEMC1 < MAC1 && SEMC4 < MAC1 && SEMC3 < MAC1 && SEMC2 < MAC1 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAC1 < MAC2 && MAC1 < MAC3 && MAC1 < MAC5 && SEMC2 < LBC2 && MAC2 < MAC3 && MAC2 < MAC5 && SEMC2 < SEMC5 && SEMC3 < LBC3 && MAC3 < MAC5 && SEMC3 < SEMC5 && SEMC4 < MAC4 && SEMC2 < SEMC4 && SEMC3 < SEMC4 && SEMC4 < SEMC5)
            {
                return CreateTrigger(context, 1.9394M, 0M, basicTrigger);
            }
            else if (SEMC5 < MAC1 && SEMC4 < MAC1 && SEMC3 < MAC1 && SEMC2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAC1 < MAC2 && MAC1 < MAC3 && MAC1 < MAC5 && SEMC2 < LBC2 && MAC2 < MAC3 && MAC2 < MAC5 && SEMC2 < SEMC5 && SEMC3 < LBC3 && MAC3 < MAC5 && SEMC3 < SEMC5 && SEMC4 < MAC4 && SEMC4 < SEMC5 && SEMC5 < LBC5)
            {
                return CreateTrigger(context, 1.9393M, 0M, basicTrigger);
            }
            else if (SEMC4 < MAC1 && SEMC3 < MAC1 && SEMC2 < MAC1 && MAL2 < MAC1 && MAL2 < MAC2 && MAL2 < MAC3 && MAL2 < MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && SEMC1 < SEMC5 && SEMC1 < MAC1 && MAC1 < MAC2 && MAC1 < MAC3 && MAC1 < MAC5 && SEMC2 < MAC2 && MAC2 < MAC3 && MAC2 < MAC5 && SEMC2 < SEMC5 && SEMC3 < MAC3 && MAC3 < MAC5 && SEMC3 < SEMC5 && SEMC4 < MAC4 && SEMC2 < SEMC4 && SEMC3 < SEMC4 && SEMC5 > SEMC4 && SEMC5 > MAC1)
            {
                return CreateTrigger(context, 1.9392M, 0M, basicTrigger);
            }
            else if (SEMC1 < MAC1 && (MAL2 < SEMC2 || MAL2 < SEMC3 || MAL2 < SEMC4) && MAL2 < MAC1 && SEMC3 < MAC3)
            {
                if (SEMC1 < LB2C5)
                {
                    return CreateTrigger(context, 1.9348M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9318M, 0M, basicTrigger);
                }
            }
            else if (C5_SEMShortTrend == 0)
            {
                if (MAL2 < MAC5)
                {
                    return CreateTrigger(context, 1.9399M, 0M, basicTrigger);
                }
                else
                {
                    return CreateTrigger(context, 1.9398M, 0M, basicTrigger);
                }
            }
            else if (C5_SEMShortTrend == 1)
            {
                return CreateTrigger(context, 1.9396M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 4.38M)
        {
            if (SEMC13 < LBC13 && SEMC21 < LBC21)
            {
                return CreateTrigger(context, 4.3899M, 0M, basicTrigger);
            }
            else if (SEMC13 < LBC8)
            {
                return CreateTrigger(context, 4.3898M, 0M, basicTrigger);
            }
            else
            {
                return CreateTrigger(context, 4.3849M, 0M, basicTrigger);
            }
        }

        if (basicTrigger == 9.16M)
        {
            return CreateTrigger(context, 9.1699M, 0M, basicTrigger);
        }

        if (basicTrigger == 9.15M)
        {
            return CreateTrigger(context, 9.1599M, 0M, basicTrigger);
        }

        if ((basicTrigger == 3 || basicTrigger == 9))
        {
            if (C5_Pivot_Status == -2 && C21_Pivot_Status == -2 && MAC1 < MAC3)
            {
                return CreateTrigger(context, 1.88M, 0M, basicTrigger);
            }
            else if (C21_Pivot_Status == -2 && MAC1 < MAC3)
            {
                return CreateTrigger(context, 1.87M, 0M, basicTrigger);
            }
            else if (C3_C13_Flag_Trend_Lower == 1)
            {
                if (MAC1 > MAC3)
                {
                    if (C5_Pivot_Status == -2 && MAC1 > MAC3)
                    {
                        return CreateTrigger(context, 9.9999M, 0M, basicTrigger);
                    }
                    else if (MAC2 < MAC3)
                    {
                        if (C21_MajorTrend == -1 || MAC5 < MAC89)
                        {
                            return CreateTrigger(context, 9.9899M, 0M, basicTrigger);
                        }
                        else if (SEMC1 > SEMC2 && SEMC2 > SEMC3)
                        {
                            if (SEMC1 > SEMC5 && SEMC2 > SEMC5)
                            {
                                return CreateTrigger(context, 9.9898M, 0M, basicTrigger);
                            }
                            else if (SEMC1 > SEMC4 && SEMC2 > SEMC4 && SEMC5 > UBC5)
                            {
                                return CreateTrigger(context, 9.9897M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 9.9896M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC1 < MAC1 && SEMC2 > SEMC4 && SEMC2 > SEMC3 && SEMC2 > MAC2 && SEMC5 > UBC5)
                        {
                            return CreateTrigger(context, 9.9895M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 9.9819M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC1 < MAC2)
                    {
                        if (MAC1 < MAC2 && MAL2 > MAC5 && MAL1 > MAC5 && L1_C3_S_cont_MAC == 0 && SEMC2 < MAC2 && MAC2 > MAC3 && SEMC3 > MAC3 && SEMC5 > UBC5 && MAL2 > C5_MAC_SupRes)
                        {
                            return CreateTrigger(context, 9.9799M, 0M, basicTrigger);
                        }
                        else if (MAC1 < MAC2 && (MAL2 < MAC5 || MAL1 < MAC5) && MAC2 > MAC3)
                        {
                            return CreateTrigger(context, 9.9749M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 9.9719M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC2 > MAC3 && MAC1 > MAC2)
                    {
                        if (SEMC5 < MAC5)
                        {
                            if (C21_MajorTrend == -1 || MAC5 < MAC89)
                            {
                                if (MAL2 < MAC5 || MAC1 < MAC5)
                                {
                                    return CreateTrigger(context, 9.9699M, 0M, basicTrigger);
                                }
                                else
                                {
                                    if (MAL2 > MAL1)
                                    {
                                        return CreateTrigger(context, 9.9698M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9697M, 0M, basicTrigger);
                                    }
                                }
                            }
                            else if (MAC3 < MAC5)
                            {
                                if (MAL2 < MAC5)
                                {
                                    if (MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && MAC4 < MAC5 && MAL2 < MAC1 && (MAL2 < MAC2 || MAL2 < MAC3 || MAL2 < MAC4 && SEMC1 < MAC2 && SEMC1 < MAC3 && SEMC1 < MAC4 && SEMC1 < LBC1 && SEMC2 > MAC2 && SEMC3 > MAC3) && SEMC2 > SEMC5 && SEMC3 > SEMC5 && MAL2 > MAC13 && MAL2 > MAC21)
                                    {
                                        return CreateTrigger(context, 9.9696M, 0M, basicTrigger);
                                    }
                                    else if (SEMC1 > LBC1 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && MAL2 < SEMC2)
                                    {
                                        return CreateTrigger(context, 9.9649M, 0M, basicTrigger);
                                    }
                                    else if (SEMC2 < MAC2 && SEMC3 < MAC3 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > SEMC3 && MAL2 < SEMC2 && MAL2 < SEMC3)
                                    {
                                        return CreateTrigger(context, 9.9648M, 0M, basicTrigger);
                                    }
                                    else if (MAC1 > MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC4 > MAC4 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && SEMC2 < SEMC3 && SEMC2 < SEMC4)
                                    {
                                        return CreateTrigger(context, 9.9647M, 0M, basicTrigger);
                                    }
                                    else if (MAL2 < MAC13 || MAL2 < MAC21 && MAL1 < MAC5 && MAC1 < MAC5 && MAC2 < MAC5 && MAC3 < MAC5 && MAC4 < MAC5 && SEMC1 < MAC5 && SEMC5 < MAC5 && SEMC2 > MAC2 && MAL2 < SEMC2)
                                    {
                                        return CreateTrigger(context, 9.9646M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9619M, 0M, basicTrigger);
                                    }
                                }
                                else if (MAL2 > MAC5)
                                {
                                    if (cParam.L2_L1_MA_Conf == 1 && MAL2 > MAC1 && MAL1 > MAC1)
                                    {
                                        return CreateTrigger(context, 9.9695M, 0M, basicTrigger);
                                    }
                                    else if (SEMC1 < MAC1 && MAL1 > MAC1 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1 && MAL2 < MAC1 && MAL2 > SEMC4 && MAC1 > MAC5 && SEMC2 > SEMC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5)
                                    {
                                        return CreateTrigger(context, 9.9694M, 0M, basicTrigger);
                                    }
                                    else if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                                    {
                                        return CreateTrigger(context, 9.9645M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9618M, 0M, basicTrigger);
                                    }
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9617M, 0M, basicTrigger);
                                }
                            }
                            else
                            {
                                return CreateTrigger(context, 9.9601M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC3 < MAC3)
                        {
                            return CreateTrigger(context, 9.9599M, 0M, basicTrigger);
                        }
                        else if (SEMC2 < MAC2)
                        {
                            if (MAL2 > MAC2 && MAL2 < SEMC1 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5 && SEMC1 > MAC1 && SEMC1 < SEMC5 && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4)
                            {
                                return CreateTrigger(context, 9.9449M, 0M, basicTrigger);
                            }
                            else if (MAL2 < MAC3 && MAL2 < C2_MAC_SupRes && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC5)
                            {
                                if (cClose > MAC2 || cHigh >= MAC2)
                                {
                                    return CreateTrigger(context, 9.9448M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9447M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAC2 && SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > C2_MAC_SupRes && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4 && SEMC3 < SEMC5 && SEMC3 > MAC3 && SEMC4 > MAC4 && SEMC5 > MAC5 && SEMC4 < SEMC5)
                            {
                                return CreateTrigger(context, 9.9499M, 0M, basicTrigger);
                            }
                            else if (MAL2 < MAC2 && SEMC1 < MAC1 && MAL2 > SEMC1 && MAL2 > MAC3 && MAL2 > LBC2 && C2_UBLB2_Dir == 1 && C3_UBLB1_Dir == -1 && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC2 < SEMC5 && SEMC3 < SEMC4 && SEMC3 < SEMC5 && SEMC4 > SEMC5)
                            {
                                return CreateTrigger(context, 9.9498M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 9.9419M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC1 < MAC1)
                        {
                            if (MAC3 < MAC5)
                            {
                                if (MAL2 > MAC5)
                                {
                                    if (cParam.L2_L1_MA_Conf == 1 && MAL2 > MAC1 && MAL1 > MAC1)
                                    {
                                        return CreateTrigger(context, 9.9399M, 0M, basicTrigger);
                                    }
                                    else if (SEMC1 < MAC1 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1 && MAL2 < MAC1 && MAL2 > SEMC4 && MAC1 > MAC5 && SEMC3 > SEMC4 && SEMC4 > SEMC5 && SEMC5 > MAC5)
                                    {
                                        return CreateTrigger(context, 9.9398M, 0M, basicTrigger);
                                    }
                                    else if (MAL2 > MAC5 && MAL1 > MAC5 && MAC1 > MAC5 && MAC2 > MAC5 && MAL2 < MAC1 && MAL2 > MAC2 && MAL2 > MAC3 && MAL2 > MAC4 && MAL1 < MAC1 && SEMC1 < MAC1 && SEMC1 > MAC2 && SEMC1 > MAC3 && SEMC1 > MAC4 && SEMC1 > MAC5 && SEMC2 > MAC2 && SEMC3 > MAC3 && SEMC2 > MAC5 && SEMC3 > MAC5 && SEMC4 > MAC5 && SEMC1 > SEMC5 && SEMC2 > SEMC5 && SEMC3 > SEMC5 && SEMC4 > SEMC5 && SEMC1 > SEMC4 && SEMC2 > SEMC4 && SEMC3 > SEMC4 && SEMC5 < UBC5 && SEMC4 < UBC4 && pC1_UBLB2_Dir == 1 && pL1_UBLB2_Dir == 1)
                                    {
                                        return CreateTrigger(context, 9.9397M, 0M, basicTrigger);
                                    }
                                    else if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                                    {
                                        if (pC1_UBLB2_Dir == -1 || pL1_UBLB2_Dir == -1)
                                        {
                                            return CreateTrigger(context, 9.9349M, 0M, basicTrigger);
                                        }
                                        else if (MAL2 < MAC2)
                                        {
                                            return CreateTrigger(context, 9.9348M, 0M, basicTrigger);
                                        }
                                        else if (SEMC2 > SEMC3)
                                        {
                                            return CreateTrigger(context, 9.9347M, 0M, basicTrigger);
                                        }
                                        else
                                        {
                                            return CreateTrigger(context, 9.9346M, 0M, basicTrigger);
                                        }
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9319M, 0M, basicTrigger);
                                    }
                                }
                                else if (MAL2 <= MAC5)
                                {
                                    if ((cParam.L2_L1_MA_Conf == -1 || MAL2 < MAC1 || MAL1 < MAC1) && MAL2 < SEMC2)
                                    {
                                        return CreateTrigger(context, 9.9345M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9318M, 0M, basicTrigger);
                                    }
                                }
                            }
                            else if (MAL2 < MAC5)
                            {
                                if (MAC1 > MAC5 && SEMC1 < LBC1 && MAL2 > SEMC1 && MAC2 > MAC5 && MAC3 > MAC4 && MAC3 > MAC5 && MAC4 > MAC5)
                                {
                                    return CreateTrigger(context, 9.9395M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9317M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAC3)
                            {
                                if (SEMC5 < SEMC8 && SEMC4 < SEMC5 && SEMC3 < SEMC4 && SEMC2 < SEMC3 && SEMC1 < LBC1)
                                {
                                    return CreateTrigger(context, 9.9344M, 0M, basicTrigger);
                                }
                                else if (MAL2 < MAC4 && MAL2 < MAC3 && (MAC1 > SEMC8 || MAC1 > SEMC13 || MAC1 > UBC13) && !(SEMC5 < SEMC8 && SEMC4 < SEMC5 && SEMC3 < SEMC4 && SEMC2 < SEMC3))
                                {
                                    return CreateTrigger(context, 9.9394M, 0M, basicTrigger);
                                }
                                else if (SEMC13 < SEMC21 && SEMC13 < UBC13 && C13_SEMTrend == -0.1M && SEMC1 < LBC1)
                                {
                                    return CreateTrigger(context, 9.9343M, 0M, basicTrigger);
                                }
                                else if (SEMC13 > SEMC21 && C1_SEMTrend != -0.1M && SEMC8 > SEMC13 && SEMC5 > SEMC13 && SEMC5 > SEMC13 && SEMC3 > SEMC13 && MAC3 > SEMC13)
                                {
                                    return CreateTrigger(context, 9.9393M, 0M, basicTrigger);
                                }
                                else if (SEMC8 > SEMC13 && MAC5 > UBC13 && SEMC8 > UBC13 && SEMC13 > UBC13 && SEMC5 < SEMC8 && SEMC5 > UBC5 && SEMC4 < SEMC8 && SEMC3 < SEMC8 && SEMC2 < SEMC8 && MAC1 < UBC5)
                                {
                                    return CreateTrigger(context, 9.9392M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9316M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAC2)
                            {
                                if (C21_Pivot_Status == -2)
                                {
                                    if (MAC2 > UBC5 && MAC1 < UBC4 && MAC1 > UBC5 && SEMC2 > MAC1)
                                    {
                                        return CreateTrigger(context, 9.9335M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9313M, 0M, basicTrigger);
                                    }
                                }
                                else if (((C5_C8_S_Cont_MAC == -1 && C3_C5_S_Cont_MAC == -1) || (C3_C5_S_Cont_MAC == -1)) && L2_MAC_Pivot_Trend == 1 && cClose < L2_MAC_SupRes && cClose < MAC2 && cClose < SEMC2 && MAC2 > UBC5 && MAC1 < UBC4 && MAC1 > UBC5 && SEMC2 > MAC1)
                                {
                                    return CreateTrigger(context, 9.9334M, 0M, basicTrigger);
                                }
                                else if (C5_MAC_Pivot_Trend == -1 && C2_MAC_Pivot_Trend == 1)
                                {
                                    if (SEMC3 > SEMC5 && SEMC3 > SEMC4 && SEMC2 < UBC1 && SEMC2 < UBC2 && SEMC2 < UBC3 && MAL2 < UBC5 && MAC1 > UBC5)
                                    {
                                        return CreateTrigger(context, 9.9387M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9386M, 0M, basicTrigger);
                                    }
                                }
                                else if (C5_MAC_Pivot_Trend == 1 && C2_MAC_Pivot_Trend == -1)
                                {
                                    return CreateTrigger(context, 9.9385M, 0M, basicTrigger);
                                }
                                else if (C5_MAC_Pivot_Trend == 1 && C2_MAC_Pivot_Trend == 1)
                                {
                                    if (MAC2 > UBC5)
                                    {
                                        if ((SEMC2 > UBC1 || SEMC2 > UBC2) && SEMC2 > UBC4 && UBC2 > UBC4 && UBC1 > UBC4 && MAC1 > UBC4)
                                        {
                                            return CreateTrigger(context, 9.9342M, 0M, basicTrigger);
                                        }
                                        else if (MAC1 > UBC4 && SEMC2 < UBC1 && SEMC2 < UBC2 && SEMC2 < UBC3 && MAL2 > UBC5)
                                        {
                                            return CreateTrigger(context, 9.9384M, 0M, basicTrigger);
                                        }
                                        else if (MAC1 > UBC4 && SEMC2 < UBC1 && SEMC2 < UBC2 && MAL2 > UBC5)
                                        {
                                            return CreateTrigger(context, 9.9383M, 0M, basicTrigger);
                                        }
                                        else if (MAC1 < UBC4 && MAL2 > UBC5 && MAC1 > UBC5 && SEMC2 > MAC1)
                                        {
                                            return CreateTrigger(context, 9.9341M, 0M, basicTrigger);
                                        }
                                        else if (MAC1 < UBC4 && MAL2 < UBC5 && MAC1 > UBC5 && SEMC2 > MAC1)
                                        {
                                            return CreateTrigger(context, 9.9339M, 0M, basicTrigger);
                                        }
                                        else if (UBC1 < UBC3 && UBC1 > UBC5 && UBC3 > UBC5)
                                        {
                                            return CreateTrigger(context, 9.9338M, 0M, basicTrigger);
                                        }
                                        else
                                        {
                                            return CreateTrigger(context, 9.9315M, 0M, basicTrigger);
                                        }
                                    }
                                    else if (SEMC2 > UBC1 || SEMC2 > UBC2 || SEMC2 > UBC3)
                                    {
                                        return CreateTrigger(context, 9.9337M, 0M, basicTrigger);
                                    }
                                    else if (UBC1 < UBC2 && UBC2 < UBC3)
                                    {
                                        return CreateTrigger(context, 9.9336M, 0M, basicTrigger);
                                    }
                                    else if (MAC1 > UBC5 && MAL2 < UBC5)
                                    {
                                        return CreateTrigger(context, 9.9382M, 0M, basicTrigger);
                                    }
                                    else if (MAC2 > UBC9 && MAC1 > UBC4)
                                    {
                                        return CreateTrigger(context, 9.9381M, 0M, basicTrigger);
                                    }
                                    else if (MAC2 > UBC7 && MAC2 > UBC8 && MAC2 > UBC9)
                                    {
                                        return CreateTrigger(context, 9.9379M, 0M, basicTrigger);
                                    }
                                    else if (MAC1 > UBC4 && MAC1 > UBC5)
                                    {
                                        return CreateTrigger(context, 9.9375M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9314M, 0M, basicTrigger);
                                    }
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9313M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAC1)
                            {
                                if (MAC1 > SEMC3 && MAC1 > SEMC4 && MAC1 > SEMC5)
                                {
                                    return CreateTrigger(context, 9.9299M, 0M, basicTrigger);
                                }
                                else if (MAC1 > SEMC4 && MAC1 > SEMC5)
                                {
                                    return CreateTrigger(context, 9.9298M, 0M, basicTrigger);
                                }
                                else if (MAC1 > SEMC5 && MAL2 > SEMC5)
                                {
                                    if (C1_C5_S_Cont_LB1 == -1 || C1_C5_Conf_SEMs == -1)
                                    {
                                        return CreateTrigger(context, 9.9297M, 0M, basicTrigger);
                                    }
                                    else if (MAL1 > UBC3)
                                    {
                                        return CreateTrigger(context, 9.9296M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 9.9295M, 0M, basicTrigger);
                                    }
                                }
                                else if (MAC1 > SEMC5 && MAL2 <= SEMC5)
                                {
                                    return CreateTrigger(context, 9.9294M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 9.9218M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < MAL1)
                            {
                                return CreateTrigger(context, 9.9288M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 9.9219M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 9.9201M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 9.9119M, 0M, basicTrigger);
                    }
                }
                else if (C5_Pivot_Status == -2 && MAC1 < MAC3)
                {
                    return CreateTrigger(context, 9.8999M, 0M, basicTrigger);
                }
                else if (C5_Pivot_Status_Odd > 0 && MAC1 < MAC3)
                {
                    if (MAC3 < MAC5 && MAC4 < MAC5 && SEMC4 < MAC1)
                    {
                        return CreateTrigger(context, 9.7999M, 0M, basicTrigger);
                    }
                    else if (MAC3 > MAC5 && MAC4 > MAC5 && MAL2 > MAC5 && MAL2 > LBC2)
                    {
                        return CreateTrigger(context, 9.7998M, 0M, basicTrigger);
                    }
                    else if (MAC3 < MAC5 && SEMC4 > MAC1 && MAL2 < MAC5 && SEMC2 < MAC2 && MAL2 < LBC2 && MAL2 < SEMC4 && MAL2 < SEMC3)
                    {
                        return CreateTrigger(context, 9.7949M, 0M, basicTrigger);
                    }
                    else if (MAC3 > MAC5 && MAC2 < MAC3 && SEMC3 < MAC3 && MAL2 > MAC5 && SEMC2 < MAC2 && MAL2 < LBC2 && MAL2 < SEMC2 && MAL2 < MAC1)
                    {
                        return CreateTrigger(context, 9.7948M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.7919M, 0M, basicTrigger);
                    }
                }
                else if (C2_MAC_Pivot_Trend == -1 && C5_MAC_Pivot_Trend == 1)
                {
                    return CreateTrigger(context, 9.8899M, 0M, basicTrigger);
                }
                else if (MAC1 < MAC3 && MAC2 > MAC3)
                {
                    if (MAC1 < MAC5)
                    {
                        return CreateTrigger(context, 9.8799M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.8798M, 0M, basicTrigger);
                    }
                }
                else if (C21_Pivot_Status == -2)
                {
                    return CreateTrigger(context, 9.8699M, 0M, basicTrigger);
                }
                else if (SEMC5 > MAC5 && (MAL2 < LBC5 || (SEML2 < LBC5 && MAL2 < LBC4)))
                {
                    return CreateTrigger(context, 9.8199M, 0M, basicTrigger);
                }
                else if (SEMC5 < MAC5)
                {
                    if (SEMC5 < MAC1 || SEMC5 < LBC5)
                    {
                        return CreateTrigger(context, 9.8599M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.8519M, 0M, basicTrigger);
                    }
                }
                else if (SEMC4 < MAC4)
                {
                    if (MAL2 > MAC4 || SEMC1 > MAC4)
                    {
                        return CreateTrigger(context, 9.8491M, 0M, basicTrigger);
                    }
                    else if (C21_MajorTrend == -1 || MAC5 < MAC89)
                    {
                        if (SEMC4 < MAC5 && SEMC4 < MAC2 && SEMC3 < LBC3 && MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 9.8499M, 0M, basicTrigger);
                        }
                        else if (SEMC1 > SEMC2 && SEMC2 > SEMC3 && SEMC3 < LBC3 && MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 9.8498M, 0M, basicTrigger);
                        }
                        else if (SEMC4 < MAC5 && (SEMC1 > SEMC4 || SEML1 > SEMC4) && SEMC3 < LBC3 && MAL2 > SEMC3)
                        {
                            return CreateTrigger(context, 9.8497M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 9.8496M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC4 < MAC5 && SEMC4 < MAC2 && SEMC3 < LBC3 && MAL2 > SEMC3)
                    {
                        return CreateTrigger(context, 9.8495M, 0M, basicTrigger);
                    }
                    else if (SEMC1 > SEMC2 && SEMC2 > SEMC3 && SEMC3 < LBC3 && MAL2 > SEMC3)
                    {
                        return CreateTrigger(context, 9.8494M, 0M, basicTrigger);
                    }
                    else if (SEMC4 < MAC5 && (SEMC1 > SEMC4 || SEML1 > SEMC4) && SEMC3 < LBC3 && MAL2 > SEMC3)
                    {
                        return CreateTrigger(context, 9.8493M, 0M, basicTrigger);
                    }
                    else if (MAL2 > MAC1 && SEMC1 > MAC1 && SEMC3 < LBC3 && MAL2 > SEMC3)
                    {
                        return CreateTrigger(context, 9.8492M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.8419M, 0M, basicTrigger);
                    }
                }
                else if (SEMC3 < MAC3)
                {
                    if (C21_MajorTrend == -1 || MAC5 < MAC89)
                    {
                        return CreateTrigger(context, 9.8399M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.8319M, 0M, basicTrigger);
                    }
                }
                else if (SEMC2 < MAC2)
                {
                    if ((SEMC2 < MAL2 && SEMC2 < LBC2) && C3_UBLB1_Dir == 1)
                    {
                        return CreateTrigger(context, 9.8299M, 0M, basicTrigger);
                    }
                    else if (SEMC2 > MAL2 && (SEMC2 > LBC2 || C3_UBLB1_Dir == -1))
                    {
                        return CreateTrigger(context, 9.8249M, 0M, basicTrigger);
                    }
                    else
                    {
                        return CreateTrigger(context, 9.8219M, 0M, basicTrigger);
                    }
                }
                else
                {
                    return CreateTrigger(context, 9.41M, 0M, basicTrigger);
                }
            }
            else if (C3_C13_Flag_Trend_Lower == -1 && C5_C21_Flag_Trend_Lower == 1)
            {
                return CreateTrigger(context, 9.79M, 0M, basicTrigger);
            }
            else if (C3_C13_Flag_Trend_Lower == -1 && C5_C21_Flag_Trend_Lower == -1)
            {
                return CreateTrigger(context, 9.29M, 0M, basicTrigger);
            }
        }

        if ((basicTrigger == 8 || basicTrigger == 2))
        {
            if (C5_C21_Flag_Trend_Lower == 1)
            {
                if (C3_C13_Flag_Trend_Lower == 1)
                {
                    if (MAL2 < LB2C5 && MAC3 > MAC13)
                    {
                        return CreateTrigger(context, 8.8989M, 0M, basicTrigger);
                    }
                    else if (C5_Pivot_Status_Odd > 0 && MAC1 < MAC3)
                    {
                        return CreateTrigger(context, 8.8991M, 0M, basicTrigger);
                    }
                    else if (MAL2 < LB2C13)
                    {
                        return CreateTrigger(context, 8.8999M, 0M, basicTrigger);
                    }
                    else if (MAL2 < LBC13)
                    {
                        return CreateTrigger(context, 8.8998M, 0M, basicTrigger);
                    }
                    else if (MAL2 < MAC13)
                    {
                        if (MAC3 < MAC13)
                        {
                            return CreateTrigger(context, 8.8997M, 0M, basicTrigger);
                        }
                        else if (MAC2 < MAC13)
                        {
                            return CreateTrigger(context, 8.8996M, 0M, basicTrigger);
                        }
                        else if (MAC1 < MAC13)
                        {
                            return CreateTrigger(context, 8.8995M, 0M, basicTrigger);
                        }
                        else if (MAL2 < MAC13)
                        {
                            return CreateTrigger(context, 8.8994M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.8993M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 >= MAC13)
                    {
                        return CreateTrigger(context, 8.8992M, 0M, basicTrigger);
                    }
                }
                else if (C3_C13_Flag_Trend_Lower == -1)
                {
                    if (MAC1 < LB2C13)
                    {
                        if (MAC2 < LB2C13 && SEMC8 < MAC2)
                        {
                            return CreateTrigger(context, 8.6999M, 0M, basicTrigger);
                        }
                        else if (LB2C13 > LBC21)
                        {
                            if (C21_UBLB1_Dir == -1)
                            {
                                if (SEMC4 < MAC1 && MAL2 > MAC1 && MAL2 > MAL1 && (MAL1 > MAC1 || SEMC1 > MAC1) && MAC1 < LBC5 && MAC1 < LBC4 && MAC1 < MAC2)
                                {
                                    return CreateTrigger(context, 8.6998M, 0M, basicTrigger);
                                }
                                else if (MAC2 < LBC5 && MAL2 > MAC1 && MAL2 > MAL1 && (MAL1 > MAC1 || SEMC1 > MAC1))
                                {
                                    return CreateTrigger(context, 8.6997M, 0M, basicTrigger);
                                }
                                else if (MAL2 < LBC2 && (MAL2 < SEMC5 || MAL2 < SEMC4 || MAL2 < SEMC3 || MAL2 < SEMC2))
                                {
                                    return CreateTrigger(context, 8.6949M, 0M, basicTrigger);
                                }
                                else if ((MAL2 < SEMC5 || MAL2 < SEMC4) && ((SEMC5 > MAC1 && MAC2 < LBC5) || SEMC4 > MAC1))
                                {
                                    return CreateTrigger(context, 8.6948M, 0M, basicTrigger);
                                }
                                else if (MAC2 < LBC5 && MAL2 < LB2C13 && MAL2 < LBC5 && (MAL2 < MAC1 || MAL1 < MAC1 || MAL2 < MAL1))
                                {
                                    return CreateTrigger(context, 8.6947M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                }
                            }
                            else if (C21_UBLB1_Dir == 1)
                            {
                                if (MAL2 > LBC21)
                                {
                                    if (SEMC4 < MAC1 && MAC1 < MAC2 && SEMC3 < MAC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4 && SEMC5 < LBC4 && (MAC1 < LBC4 || (SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4)))
                                    {
                                        return CreateTrigger(context, 8.6996M, 0M, basicTrigger);
                                    }
                                    else if (SEMC4 > MAC1 && MAL2 < LBC2)
                                    {
                                        return CreateTrigger(context, 8.6946M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                    }
                                }
                                else if (MAL2 < LBC21 && C21_UBLB2_Dir == -1)
                                {
                                    if (SEMC4 < MAC1 && MAL2 > LBC2)
                                    {
                                        return CreateTrigger(context, 8.6995M, 0M, basicTrigger);
                                    }
                                    else if (SEMC4 > MAC1 && MAL2 < LBC2)
                                    {
                                        return CreateTrigger(context, 8.6945M, 0M, basicTrigger);
                                    }
                                    else if (MAC2 < LBC5 && SEMC5 > MAC1 && MAL2 < LBC2)
                                    {
                                        return CreateTrigger(context, 8.6944M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                    }
                                }
                                else if (MAL2 < LBC21 && C21_UBLB2_Dir == 1)
                                {
                                    if (MAL2 < LBC2 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5)
                                    {
                                        return CreateTrigger(context, 8.6943M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                    }
                                }
                            }
                        }
                        else if (LB2C13 <= LBC21 && LB2C13 > LB2C21)
                        {
                            if (MAL2 < LBC21 && C21_UBLB2_Dir == -1)
                            {
                                if (MAC2 < LBC5 && MAL2 < LBC2 && SEMC3 < SEMC5)
                                {
                                    return CreateTrigger(context, 8.6942M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < LBC21 && MAL2 > LB2C21 && C21_UBLB2_Dir == 1)
                            {
                                if (MAC2 < LBC5 && MAL2 < LBC2 && SEMC5 < LBC2 && SEMC2 < LBC2 && MAL2 < SEMC2 && MAL2 < SEMC5)
                                {
                                    return CreateTrigger(context, 8.6941M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 < LB2C21)
                            {
                                if (MAL2 < LBC2 && LBC2 < LB2C21 && MAC2 < LBC5 && SEMC5 < LB2C21 && MAL2 < SEMC5)
                                {
                                    return CreateTrigger(context, 8.6939M, 0M, basicTrigger);
                                }
                                else if (MAL2 < LBC2 && LBC2 < LB2C21 && MAC2 > LBC5 && SEMC4 < LB2C21 && MAL2 < SEMC4 && MAL2 < SEMC3)
                                {
                                    return CreateTrigger(context, 8.6938M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                                }
                            }
                            else
                            {
                                return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                            }
                        }
                        else if (LB2C13 < LB2C21)
                        {
                            return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.69M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < LB2C13)
                    {
                        if (C21_UBLB1_Dir == -1)
                        {
                            if (MAC2 > LBC5 && SEMC4 < MAC1 && MAC1 < MAC2 && SEMC3 < MAC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4 && SEMC5 < LBC4 && (MAC1 < LBC4 || (SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4)) && LB2C13 < MAC21)
                            {
                                return CreateTrigger(context, 8.6899M, 0M, basicTrigger);
                            }
                            else if (((MAC1 > LBC5 && SEMC2 > LBC4) || MAC1 > MAC2) && SEMC5 < MAC1 && SEMC4 < MAC1 && SEMC5 < LBC2 && SEMC4 < LBC2)
                            {
                                return CreateTrigger(context, 8.6849M, 0M, basicTrigger);
                            }
                            else if (LBC5 > LB2C13 && LB2C13 > MAC21 && SEMC4 < MAC1 && SEMC3 > SEMC4 && MAL2 < LBC2)
                            {
                                return CreateTrigger(context, 8.6848M, 0M, basicTrigger);
                            }
                            else if (MAL2 < LBC2 && MAC2 < LBC5 && SEMC5 > MAC1 && MAL2 < SEMC5 && SEMC4 < SEMC5 && SEMC3 < SEMC5 && SEMC2 < SEMC4 && SEMC1 < SEMC5 && SEMC4 < LBC2)
                            {
                                return CreateTrigger(context, 8.6847M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.68M, 0M, basicTrigger);
                            }
                        }
                        else if (C21_UBLB1_Dir == 1)
                        {
                            if (MAL2 <= LBC21)
                            {
                                if (SEMC4 < MAC1 && MAC1 < MAC2 && SEMC3 < MAC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4 && SEMC5 < LBC4 && (MAC1 < LBC4 || (SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4)))
                                {
                                    return CreateTrigger(context, 8.6898M, 0M, basicTrigger);
                                }
                                else if (((MAC1 > LBC5 && SEMC2 > LBC4) || MAC1 > MAC2) && SEMC5 < MAC1 && SEMC4 < MAC1 && SEMC5 < LBC2 && SEMC4 < LBC2)
                                {
                                    return CreateTrigger(context, 8.6846M, 0M, basicTrigger);
                                }
                                else if (SEMC4 > MAC1 && MAL2 < LBC2 && MAC1 < LBC4)
                                {
                                    return CreateTrigger(context, 8.6845M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.68M, 0M, basicTrigger);
                                }
                            }
                            else if (MAL2 > LBC21)
                            {
                                if (SEMC4 < MAC1 && MAC1 < MAC2 && SEMC3 < MAC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4 && SEMC5 < LBC4 && (MAC1 < LBC4 || (SEMC2 < LBC4 && SEMC3 < LBC4 && SEMC4 < LBC4)))
                                {
                                    return CreateTrigger(context, 8.6897M, 0M, basicTrigger);
                                }
                                else if (((MAC1 > LBC5 && SEMC2 > LBC4) || MAC1 > MAC2) && SEMC5 < MAC1 && SEMC4 < MAC1 && SEMC5 < LBC2 && SEMC4 < LBC2)
                                {
                                    return CreateTrigger(context, 8.6844M, 0M, basicTrigger);
                                }
                                else if (SEMC4 > MAC1 && MAL2 < LBC2 && MAC1 < LBC4)
                                {
                                    return CreateTrigger(context, 8.6843M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.68M, 0M, basicTrigger);
                                }
                            }
                        }
                        else if (MAL2 < LB2C21)
                        {
                            return CreateTrigger(context, 8.68M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.68M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < LBC13 && MAL2 > LB2C13)
                    {
                        if (MAC1 < LBC13)
                        {
                            return CreateTrigger(context, 8.6799M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.67M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 8.63M, 0M, basicTrigger);
                    }
                }
            }
            else if (C5_C21_Flag_Trend_Lower == -1)
            {
                if (C3_C13_Flag_Trend_Lower == 1)
                {
                    return CreateTrigger(context, 8.1198M, 0M, basicTrigger);
                }
                else if (C3_C13_Flag_Trend_Lower == -1)
                {
                    if (C2_Pivot_Status_Odd > 0 && MAC2 < MAC5)
                    {
                        return CreateTrigger(context, 8.2998M, 0M, basicTrigger);
                    }
                    else if (MAC2 < LB2C21)
                    {
                        if (C1_C5_LLB2_Cont_UB1 == 1 && SEMC1 < SEMC5 && SEMC2 < SEMC5 && SEMC3 < SEMC5 && SEMC4 < SEMC5 && SEMC2 < SEMC3 && MAL2 < MAC1 && MAL1 < MAC1 && SEMC2 < LBC2)
                        {
                            return CreateTrigger(context, 8.9999M, 0M, basicTrigger);
                        }
                        else if (MAC3 < LB2C21 && (MAL2 > SEMC1 || MAL2 > SEMC2) && C3_C5_L_Cont_UB1 == 1 && SEMC13 < LB2C21 && LB2C13 < LB2C21)
                        {
                            return CreateTrigger(context, 8.9998M, 0M, basicTrigger);
                        }
                        else if (MAC3 < LB2C21 && (MAL2 > MAC3 || MAL1 > MAC3 || MAC1 > MAC3 || MAC2 > MAC3))
                        {
                            return CreateTrigger(context, 8.9997M, 0M, basicTrigger);
                        }
                        else if (MAC2 < LBC5 && LBC5 < LB2C21)
                        {
                            if (C2_Pivot_Status_Long == 0 && SEMC2 < LBC2 && SEMC1 < MAC1 && MAL2 < MAC1 && MAL2 > SEMC1 && MAL2 > SEMC2 && MAC3 > LB2C21 && LB2C13 < LB2C21 && SEMC13 > LB2C21 && SEMC5 < LB2C21)
                            {
                                return CreateTrigger(context, 8.9996M, 0M, basicTrigger);
                            }
                            else if (MAC1 > MAC2 && SEMC2 > MAC2 && (SEMC2 < UBC2 || SEMC1 < MAC1 || MAL2 > MAC3) && MAL2 > MAC2 && C2_SEMTrend != -1)
                            {
                                return CreateTrigger(context, 8.9995M, 0M, basicTrigger);
                            }
                            else if (C2_Pivot_Status_Long > 0)
                            {
                                if (C5_Pivot_Status_Long > 0)
                                {
                                    return CreateTrigger(context, 8.9994M, 0M, basicTrigger);
                                }
                                else if (MAC3 < LB2C21 && (SEMC2 < LBC2 || SEMC2 < MAC2) && MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 < MAC2 && MAL1 < LB2C13)
                                {
                                    return CreateTrigger(context, 8.9993M, 0M, basicTrigger);
                                }
                                else if (MAL1 > MAC1 && MAL2 > MAC1)
                                {
                                    return CreateTrigger(context, 8.9992M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.9991M, 0M, basicTrigger);
                                }
                            }
                            else if (MAC3 < LB2C21)
                            {
                                return CreateTrigger(context, 8.9989M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9988M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 8.99M, 0M, basicTrigger);
                        }
                    }
                    else if (C2_Pivot_Status_Long == 2 && C2_MAC_SupRes > C5_MAC_SupRes)
                    {
                        if (C5_Pivot_Status_Long > 0)
                        {
                            if (C5_UBLB1_Dir == 1 && (MAL2 > SEMC2 || MAL2 > SEMC3 || MAL2 > SEMC4 || MAL2 > SEMC5))
                            {
                                return CreateTrigger(context, 8.9899M, 0M, basicTrigger);
                            }
                            else if (C5_UBLB1_Dir == -1 && MAL2 < SEMC2 && MAL2 > MAC1 && MAL2 > MAC2 && MAL2 > MAC5 && SEMC2 > MAC2 && SEMC1 > MAC1)
                            {
                                return CreateTrigger(context, 8.9849M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.98M, 0M, basicTrigger);
                            }
                        }
                        else if (C5_Pivot_Status_Long == 0)
                        {
                            if (MAL2 > LBC2 && MAL2 < MAC2 && SEMC1 < MAC1 && MAL2 < MAC1 && MAL2 > SEMC1)
                            {
                                return CreateTrigger(context, 8.9898M, 0M, basicTrigger);
                            }
                            else if (MAL2 < LBC2 && MAC1 < LBC5 && MAL2 < SEMC13 && MAC2 < MAC3 && MAC3 < MAC5 && SEMC1 < MAC1 && SEMC2 < MAC2)
                            {
                                return CreateTrigger(context, 8.9848M, 0M, basicTrigger);
                            }
                            else if (MAL2 < LBC2 && MAC1 > LBC5 && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5)
                            {
                                return CreateTrigger(context, 8.9847M, 0M, basicTrigger);
                            }
                            else if (MAL2 > MAC2 && MAL2 > MAC1 && SEMC1 > MAC1 && MAL2 < SEMC1 && UBC2 < MAC5 && MAC3 < MAC5)
                            {
                                return CreateTrigger(context, 8.9846M, 0M, basicTrigger);
                            }
                            else if (MAL2 > MAC1 && SEMC1 > MAC1 && MAL2 < SEMC1 && MAC1 < LBC5 && UB2C1 < MAC2 && MAC2 < MAC3)
                            {
                                return CreateTrigger(context, 8.9845M, 0M, basicTrigger);
                            }
                            else if (C5_Pivot_Status_Long == 0)
                            {
                                return CreateTrigger(context, 8.98M, 0M, basicTrigger);
                            }
                        }
                    }
                    else if (MAC1 < LB2C21 && !(MAC3 < MAC5 && MAC3 < LBC13))
                    {
                        return CreateTrigger(context, 8.9799M, 0M, basicTrigger);
                    }
                    else if (MAC2 < MAC5 && MAC2 < LBC13)
                    {
                        return CreateTrigger(context, 8.9699M, 0M, basicTrigger);
                    }
                    else if (MAC2 > MAC5)
                    {
                        if (C2_C5_Conf_MAs == -1)
                        {
                            if (MAL2 > SEMC1 && MAL2 < MAC1 && MAC2 > MAC3)
                            {
                                return CreateTrigger(context, 8.9599M, 0M, basicTrigger);
                            }
                            else if (MAL2 < SEMC1 && MAL2 > MAC1 && SEMC3 < SEMC5 && SEMC3 < MAC3 && SEMC5 < MAC5 && MAL2 < SEMC3 && MAL2 < SEMC5)
                            {
                                return CreateTrigger(context, 8.9549M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9519M, 0M, basicTrigger);
                            }
                        }
                        else if ((C1_C2_S_Cont_MAC == -1 || C1_C3_S_Cont_MAC == -1) && MAL2 < SEMC2 && MAL2 < SEMC1 && SEML1 < SEMC2 && SEMC1 < SEMC2 && C2_UBLB2_Dir == -1 && C1_UBLB2_Dir == -1 && SEMC2 > UBC2 && (MAC2 < C2_MAC_SupRes || MAL2 < MAC21))
                        {
                            return CreateTrigger(context, 8.9548M, 0M, basicTrigger);
                        }
                        else if ((L1_C1_S_Cont_LB1 == -1 || (L1_C2_S_Cont_LB1 == -1 && L1_UBLB1_Dir == -1)) && MAL2 < SEMC1 && C2_UBLB2_Dir == -1 && C1_UBLB2_Dir == -1 && SEMC2 > UBC2 && (MAC2 < C2_MAC_SupRes || MAL2 < MAC21))
                        {
                            return CreateTrigger(context, 8.9547M, 0M, basicTrigger);
                        }
                        else if ((C2_C3_S_Cont_MAC == -1 || C2_C4_S_Cont_MAC == -1) && C2_UBLB2_Dir == -1 && SEMC2 > UBC2 && (MAC2 < C2_MAC_SupRes || MAL2 < MAC21))
                        {
                            return CreateTrigger(context, 8.9546M, 0M, basicTrigger);
                        }
                        else if (cParam.SupRes_touch == 1 && C2_Pivot_Status_Long > 0 && MAC3 > MAC5)
                        {
                            if (SEMC5 < UBC5)
                            {
                                return CreateTrigger(context, 8.9598M, 0M, basicTrigger);
                            }
                            else if (SEML2 > MAL2 && SEMC2 < SEMC3 && MAL2 > MAC1 && SEMC1 > MAC1)
                            {
                                return CreateTrigger(context, 8.9545M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9518M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC3 < MAC3)
                        {
                            if (MAL2 > SEMC2 && MAL2 < MAC3 && MAL2 > LBC5 && MAL2 > MAC1)
                            {
                                return CreateTrigger(context, 8.9597M, 0M, basicTrigger);
                            }
                            else if (MAL2 > SEMC2 && MAL2 > LBC2 && MAL2 < MAC3 && MAL2 > LBC5 && MAL2 > SEMC1 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC2)
                            {
                                return CreateTrigger(context, 8.95969M, 0M, basicTrigger);
                            }
                            else if (MAL2 > SEMC2 && MAL2 < LBC2 && MAL2 < MAC3 && MAL2 > LBC5 && MAL2 > SEMC1 && SEMC1 > SEMC2 && SEMC2 < SEMC3 && SEMC2 < MAC2 && SEMC1 < MAC1 && SEMC2 < LBC2 && SEMC5 > MAC5 && SEMC3 < MAC5)
                            {
                                return CreateTrigger(context, 8.95968M, 0M, basicTrigger);
                            }
                            else if (MAC1 < MAC2 && SEMC2 < LBC2 && MAL2 < LBC2 && MAL2 < MAC3 && SEMC1 < SEMC2 && SEMC2 < SEMC3 && SEMC2 < MAC2 && SEMC1 < MAC1)
                            {
                                return CreateTrigger(context, 8.9544M, 0M, basicTrigger);
                            }
                            else if (MAC1 > MAC2 && SEMC2 > UBC2 && SEMC1 < SEMC2 && MAL2 > MAC3 && SEML2 < MAC3)
                            {
                                return CreateTrigger(context, 8.9543M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9517M, 0M, basicTrigger);
                            }
                        }
                        else if (MAC1 < MAC2)
                        {
                            if (SEMC2 < LBC2 && SEMC2 < MAC1 && SEMC3 > MAC3 && SEMC3 > MAC2 && MAL2 < LBC2)
                            {
                                return CreateTrigger(context, 8.9595M, 0M, basicTrigger);
                            }
                            else if (SEMC2 < MAC2 && SEMC2 < MAC1 && SEMC2 > LBC2 && MAL2 > SEMC2 && SEMC3 > MAC3)
                            {
                                return CreateTrigger(context, 8.9594M, 0M, basicTrigger);
                            }
                            else if (SEMC2 < MAC2 && SEMC2 > LBC2 && MAL2 > SEMC2 && MAL1 > SEMC2 && SEMC3 > MAC3 && MAL2 > MAC1 && MAL1 > MAC1)
                            {
                                return CreateTrigger(context, 8.9593M, 0M, basicTrigger);
                            }
                            else if (SEMC2 < LBC2 && SEMC3 < MAC3 && MAL2 < LBC2 && MAL2 < SEMC2)
                            {
                                return CreateTrigger(context, 8.9542M, 0M, basicTrigger);
                            }
                            else if ((SEMC2 > LBC2 || SEMC2 > MAC1) && SEMC3 > MAC3 && (MAL2 < SEMC2 || MAL1 < SEMC2))
                            {
                                return CreateTrigger(context, 8.9541M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9516M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < MAC3)
                        {
                            if (SEMC1 < MAC1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAC2 > MAC3 && MAC1 > MAC3 && MAL2 > SEMC1 && SEMC1 < LBC1 && C5_SEMTrend != -0.1M && SEMC3 > SEMC5 && SEMC2 > SEMC5 && SEMC2 < SEMC3 && SEMC5 > MAC5)
                            {
                                return CreateTrigger(context, 8.9592M, 0M, basicTrigger);
                            }
                            else if (SEMC2 < MAC2 && (SEMC2 > LBC2 || SEMC2 > MAC1) && SEMC3 > MAC3 && MAL2 < SEMC2 && C5_SEMTrend == -0.1M && SEMC3 < SEMC5 && SEMC2 < SEMC5 && SEMC2 < SEMC3 && SEMC5 > UBC5)
                            {
                                return CreateTrigger(context, 8.9539M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9515M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < MAC2)
                        {
                            if (SEMC1 < MAC1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAC2 > MAC3 && MAC1 > MAC3 && MAL2 > SEMC1 && SEMC1 < LBC1 && SEMC5 > MAC5 && SEMC3 > SEMC5 && SEMC2 > SEMC5 && SEMC2 < SEMC3 && SEMC2 < SEMC4 && SEMC3 > SEMC4)
                            {
                                return CreateTrigger(context, 8.9591M, 0M, basicTrigger);
                            }
                            else if (SEMC1 < MAC1 && SEMC1 > LBC1 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 < SEMC2 && (SEMC2 < SEMC3 || (C5_SEMTrend == -0.1M && SEMC5 < UBC5 && SEMC1 < SEMC3)) && MAL2 < SEMC3)
                            {
                                return CreateTrigger(context, 8.9538M, 0M, basicTrigger);
                            }
                            else if (SEMC1 < MAC1 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 < SEMC2 && MAL2 < SEMC3 && SEMC2 > SEMC4)
                            {
                                return CreateTrigger(context, 8.9537M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9514M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < MAC1)
                        {
                            if (SEMC5 < MAC5)
                            {
                                if (cParam.C1_SEM_LB1 == 1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAC2 > MAC3 && MAC1 > MAC3 && MAL2 > SEMC1 && SEMC3 > SEMC5 && SEMC2 > SEMC5 && SEMC2 > SEMC4 && SEMC3 > SEMC4 && C1_C5_Conf_SEMs == 1)
                                {
                                    return CreateTrigger(context, 8.9589M, 0M, basicTrigger);
                                }
                                else if (SEMC1 < MAC1 && SEMC1 > LBC1 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 < SEMC2 && MAL2 < SEMC3 && cParam.C1_SEM_LB1 == 0)
                                {
                                    return CreateTrigger(context, 8.9536M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.9513M, 0M, basicTrigger);
                                }
                            }
                            else if (SEMC1 < LBC1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAC2 > MAC3 && MAC1 > MAC3 && MAL2 > SEMC1 && SEMC5 > MAC5 && SEMC3 > SEMC5 && SEMC2 > SEMC5 && SEMC3 > SEMC4 && SEMC2 > SEMC4 && C1_C5_Conf_SEMs == 1)
                            {
                                return CreateTrigger(context, 8.9588M, 0M, basicTrigger);
                            }
                            else if (SEMC1 < MAC1 && SEMC1 > LBC1 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 < SEMC2 && MAL2 < SEMC3 && cParam.C1_SEM_LB1 == 0 && (SEMC5 < MAC5 || SEMC2 < SEMC3 || (C5_SEMTrend == -0.1M && SEMC5 < UBC5 && SEMC1 < SEMC3) || SEMC3 < SEMC5 || MAC2 < MAC3 || (SEMC4 < SEMC5 && SEMC4 > MAC5 && SEMC5 > MAC5)))
                            {
                                return CreateTrigger(context, 8.9535M, 0M, basicTrigger);
                            }
                            else if (C1_C5_Conf_SEMs == -1 && SEMC1 < SEMC5 && SEMC1 < LBC1 && SEMC3 > MAC3 && SEMC2 > MAC2 && MAL2 < SEMC2 && MAL2 < SEMC3 && MAL2 < SEMC4 && MAL2 < SEMC5 && (SEMC2 > SEMC3 || SEMC2 > SEMC4))
                            {
                                return CreateTrigger(context, 8.9534M, 0M, basicTrigger);
                            }
                            else if (SEMC1 < MAC1 && SEMC2 > MAC2 && SEMC3 > MAC3 && MAC2 > MAC3 && MAC1 > MAC3 && MAL2 > SEML1 && SEMC5 > MAC5 && SEMC3 > SEMC5 && SEMC2 > SEMC5 && SEMC1 > SEMC4 && SEMC3 > SEMC4 && SEMC2 > SEMC4 && SEMC2 > SEMC3 && C1_C5_Conf_SEMs == 1)
                            {
                                return CreateTrigger(context, 8.9587M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9512M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 < MAL1)
                        {
                            if ((MAC3 > MAC5 && SEMC5 > MAC5) || MAC1 > UBC5)
                            {
                                if (MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > SEMC4 && MAL2 > SEMC5)
                                {
                                    return CreateTrigger(context, 8.9586M, 0M, basicTrigger);
                                }
                                else if (MAC1 > UBC5 && MAL1 > MAC1 && MAC1 > MAC2 && MAC2 > MAC3 && SEMC3 > SEMC4 && SEMC4 > SEMC5)
                                {
                                    return CreateTrigger(context, 8.9585M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.9511M, 0M, basicTrigger);
                                }
                            }
                            else if (MAC1 < UBC5 && (MAC3 < MAC5 || SEMC5 < MAC5) && MAL2 < SEMC1 && MAL2 < SEMC2 && MAL2 < SEMC3 && SEMC1 < SEMC2)
                            {
                                return CreateTrigger(context, 8.9533M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9509M, 0M, basicTrigger);
                            }
                        }
                        else if (MAL2 > MAL1)
                        {
                            if (MAC3 > MAC5 && SEMC5 > MAC5)
                            {
                                return CreateTrigger(context, 8.9584M, 0M, basicTrigger);
                            }
                            else if (SEMC5 > MAC5 && MAL2 > SEMC2 && MAL2 > SEMC3 && MAL2 > SEMC4 && MAL2 > SEMC5)
                            {
                                return CreateTrigger(context, 8.9583M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.9508M, 0M, basicTrigger);
                            }
                        }
                        else
                        {
                            return CreateTrigger(context, 8.95M, 0M, basicTrigger);
                        }
                    }
                    else if (MAC5 < LBC21)
                    {
                        return CreateTrigger(context, 8.9399M, 0M, basicTrigger);
                    }
                    else if (MAC2 < LBC13)
                    {
                        return CreateTrigger(context, 8.9299M, 0M, basicTrigger);
                    }
                    else if (MAC1 < LBC13)
                    {
                        return CreateTrigger(context, 8.9199M, 0M, basicTrigger);
                    }
                    else if (MAC5 < LBC13)
                    {
                        if (SEMC5 <= SEMC13)
                        {
                            if (MAL2 > MAC1 && SEML2 > MAC1)
                            {
                                if (L2_MAC_Pivot_Trend == 1)
                                {
                                    return CreateTrigger(context, 8.2989M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.298M, 0M, basicTrigger);
                                }
                            }
                            else if (C2_Pivot_Status_Long > 0 || MAC3 < C2_MAC_SupRes)
                            {
                                if (MAL2 < LB2C13)
                                {
                                    if (L2_MAC_Pivot_Trend == 1)
                                    {
                                        return CreateTrigger(context, 8.2979M, 0M, basicTrigger);
                                    }
                                    else
                                    {
                                        return CreateTrigger(context, 8.297M, 0M, basicTrigger);
                                    }
                                }
                                else if (SEMC1 < SEMC5)
                                {
                                    return CreateTrigger(context, 8.296M, 0M, basicTrigger);
                                }
                                else if (SEMC1 < SEMC2 && SEMC2 < MAC2)
                                {
                                    return CreateTrigger(context, 8.295M, 0M, basicTrigger);
                                }
                                else
                                {
                                    return CreateTrigger(context, 8.294M, 0M, basicTrigger);
                                }
                            }
                            else if (SEMC5 < LB2C13 && LB2C13 < LB2C21)
                            {
                                return CreateTrigger(context, 8.2939M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.293M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC5 > SEMC13)
                        {
                            if (MAL2 > MAC5 && (SEMC1 > MAC1 && SEML2 > MAL2))
                            {
                                return CreateTrigger(context, 8.292M, 0M, basicTrigger);
                            }
                            else if (MAL2 > MAC5 && (SEMC1 < MAC1 && SEML2 < MAL2))
                            {
                                return CreateTrigger(context, 8.2919M, 0M, basicTrigger);
                            }
                            else if (SEMC3 < SEMC5)
                            {
                                return CreateTrigger(context, 8.291M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.289M, 0M, basicTrigger);
                            }
                        }
                    }
                    else if (MAC1 < LB2C13 && cParam.C1_LB2C13_confirm == -1)
                    {
                        if (MAL2 > LB2C13)
                        {
                            if (SEML2 < MAL2)
                            {
                                return CreateTrigger(context, 8.2879M, 0M, basicTrigger);
                            }
                            else
                            {
                                return CreateTrigger(context, 8.2878M, 0M, basicTrigger);
                            }
                        }
                        else if (SEMC3 < SEMC5)
                        {
                            return CreateTrigger(context, 8.2877M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.287M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < LB2C13)
                    {
                        if (SEMC1 > LBC1 || SEMC1 > SEMC5 || SEML2 < MAL2)
                        {
                            return CreateTrigger(context, 8.286M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.285M, 0M, basicTrigger);
                        }
                    }
                    else if (C5_C13_L_Cont_UB2 == 1)
                    {
                        if (SEMC5 < LB2C13 && LB2C13 < LB2C21)
                        {
                            return CreateTrigger(context, 8.2849M, 0M, basicTrigger);
                        }
                        else if (MAC1 > MAC5)
                        {
                            return CreateTrigger(context, 8.2848M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.284M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 < LB2C13 && LB2C13 < LB2C21)
                    {
                        if (SEMC3 > SEMC5)
                        {
                            return CreateTrigger(context, 8.283M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.282M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC5 < LB2C13 && MAL2 > MAC55)
                    {
                        if (SEMC3 > SEMC5)
                        {
                            return CreateTrigger(context, 8.275M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.274M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC3 < LB2C13 && LB2C13 < LB2C21)
                    {
                        if ((MAL2 > SEMC1 && MAL2 > SEMC2 && MAL2 > SEMC3) || (SEMC1 < MAC1 && SEMC2 < MAC2 && SEMC3 < MAC3))
                        {
                            return CreateTrigger(context, 8.281M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.279M, 0M, basicTrigger);
                        }
                    }
                    else if (SEMC3 > SEMC5 && (MAL2 > MAC1 || SEMC1 > MAC1))
                    {
                        if (C2_C8_L_Cont_UB2 == 1 || C3_C8_L_Cont_UB2 == 1 || C3_C5_L_Cont_UB1 == 1)
                        {
                            return CreateTrigger(context, 8.2789M, 0M, basicTrigger);
                        }
                        else if (SEMC3 > MAC3 && SEMC5 > MAC5)
                        {
                            return CreateTrigger(context, 8.2788M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.278M, 0M, basicTrigger);
                        }
                    }
                    else if (MAL2 < SEMC13 && MAL2 < SEMC21 && SEMC13 < SEMC21 && MAC5 < MAC13 && ((MAL2 < MAC1 && MAL2 < MAC1) || C2_UBLB1_Dir == -1))
                    {
                        return CreateTrigger(context, 8.277M, 0M, basicTrigger);
                    }
                    else if (L2_MAC_Pivot_Trend == 1)
                    {
                        if (SEMC3 < MAC3 && (SEMC1 < MAC1 || SEML2 < MAL2))
                        {
                            return CreateTrigger(context, 8.2769M, 0M, basicTrigger);
                        }
                        else
                        {
                            return CreateTrigger(context, 8.276M, 0M, basicTrigger);
                        }
                    }
                    else
                    {
                        return CreateTrigger(context, 8.271M, 0M, basicTrigger);
                    }
                }
            }
        }

        return CreateTrigger(context, 0M, 0M, basicTrigger);
    }

    /// <summary>
    /// Selects the Trigger Candidate 
    /// </summary>
    /// <param name="context">Current bar inputs and study parameters, available for custom selection rules.</param>
    /// <param name="triggers">Candidates in priority order. This method does not reorder or modify them.</param>
    /// <returns>The selected candidate, or null when the candidate list is empty.</returns>
    public override L2Trigger? SelectTrigger(L2TriggerContext context, IReadOnlyList<L2Trigger> triggers)
    {
        // An empty list has no candidate to select or use as a fallback.
        if (triggers.Count == 0)
            return null;

        //// --------------------------------------------------------------------------------------
        //// Example selection rule: Select the first long candidate,
        //// or the first short candidate when no long exists.
        ////---------------------------------------------------------------------------------------
        //// Long candidates take priority over all short and zero-side candidates.
        //// Scanning from the beginning preserves the supplied order among longs.
        //foreach (var candidate in triggers)
        //{
        //    if (candidate.Side > 0)
        //        return candidate;
        //}

        //// No long was found. Select the first short in the supplied order.
        //foreach (var candidate in triggers)
        //{
        //    if (candidate.Side < 0)
        //        return candidate;
        //}

        // Retain the first candidate as the default selection.
        return triggers[0];
    }

}
