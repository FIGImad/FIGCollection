using FIGCommon.Utilities;

namespace FIG.Studies;

public partial class L2RuntimeRules : L2RuleScript
{
    private bool StopRule_Negative_7_7449(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        {
            if (cLow >= SEMC5)
            {
                cParam.SideCounter = 7.7449M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_Negative_4_3449(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal SEMC21 = (decimal?)cStudy[FieldKeys.StudySEM__C21] ?? 0.0M;
        {
            if (SEMC3 > SEMC21 && cLow > SEMC21 && cLow > SEMC3)
            {
                cParam.SideCounter = 4.3449M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_Negative_4_3349(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal MAC3 = (decimal?)cStudy[FieldKeys.StudyMA__C3] ?? 0.0M;
        {
            if (cLow > MAC3)
            {
                cParam.SideCounter = 4.3349M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_1_9894(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        if (SEML2 > UBC1 && cHigh < SEML2)
        {
            cParam.SideCounter = 1.9894M;
            return true;
        }
    
        return false;
    }

    private bool StopRule_4_0199(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        {
            if (cLow >= Math.Max(MAC5, SEMC5))
            {
                cParam.SideCounter = 4.0199M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_0299(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal UBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_UB] ?? 0.0M;
        {
            if (cHigh >= UBC5)
            {
                cParam.SideCounter = 4.0299M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_2799(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < MAL3 && MAL3 > UBC1)
            {
                cParam.SideCounter = 4.2799M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_3292(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        {
            if (cHigh < SEML1 && cHigh < SEML2)
            {
                cParam.SideCounter = 4.3292M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_6895(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        {
            if (cHigh < SEMC1 && SEMC1 > LBC1)
            {
                cParam.SideCounter = 4.6895M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_6897(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal cClose = cStudy.Price.Close;
        {
            if (cClose < SEML2 && SEML2 < MAC1)
            {
                cParam.SideCounter = 4.6897M;
                return true;
            }
    
            if (cClose < SEML1 && SEML2 > MAC1)
            {
                cParam.SideCounter = 4.6897M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_7446(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal MAC3 = (decimal?)cStudy[FieldKeys.StudyMA__C3] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal cClose = cStudy.Price.Close;
        {
            if (cClose < SEML2 && SEML2 > MAC3)
            {
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_7799(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        {
            if (cHigh >= MAC2)
            {
                cParam.SideCounter = 4.7799M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8296(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < MAL3 && MAL3 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh < SEMC1 && SEMC1 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh >= MAC5)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8297(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal LBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_LB] ?? 0.0M;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh >= UBC1 && UBC1 > LBC5)
            {
                cParam.SideCounter = 4.8297M;
                return true;
            }
    
            if (cHigh >= SEMC5 && UBC1 < LBC5)
            {
                cParam.SideCounter = 4.8297M;
                return true;
            }
    
            if (cHigh >= MAC5)
            {
                cParam.SideCounter = 4.8297M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8298(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < MAL3 && MAL3 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh < SEMC1 && SEMC1 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh >= MAC5)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8299(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < MAL3 && MAL3 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh < SEMC1 && SEMC1 > UBC1)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
    
            if (cHigh >= MAC5)
            {
                cParam.SideCounter = 4.8298M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8396(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < MAL3 && MAL3 > UBC1)
            {
                cParam.SideCounter = 4.8396M;
                return true;
            }
    
            if (cHigh < SEMC1 && SEMC1 > UBC1)
            {
                cParam.SideCounter = 4.8396M;
                return true;
            }
    
            if (cHigh >= MAC5)
            {
                cParam.SideCounter = 4.8396M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8397(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal LBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_LB] ?? 0.0M;
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        {
            if (cLow > MAC2 && cLow > LBC5)
            {
                cParam.SideCounter = 4.8399M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8398(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal LBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_LB] ?? 0.0M;
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        {
            if (cLow > MAC2 && cLow > LBC5)
            {
                cParam.SideCounter = 4.8399M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8399(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cLow = cStudy.Price.Low;
        decimal LBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_LB] ?? 0.0M;
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        {
            if (cLow > MAC2 && cLow > LBC5)
            {
                cParam.SideCounter = 4.8399M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_4_8898(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        if (SEMC1 > UBC1 && cHigh < SEMC1)
        {
            cParam.SideCounter = 4.8898M;
            return true;
        }
    
        return false;
    }

    private bool StopRule_6_5198(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < SEML1 && SEML1 > UBC1)
            {
                cParam.SideCounter = 6.5198M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_6_7696(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        {
            if (cHigh < SEMC1 && SEMC1 > SEMC5)
            {
                cParam.SideCounter = 6.7697M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_6_7697(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        {
            if (cHigh < SEMC1 && SEMC1 > SEMC5)
            {
                cParam.SideCounter = 6.7697M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_7_1599(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < SEMC1 && SEMC1 > UBC1)
            {
                cParam.SideCounter = 7.1599M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_7_1795(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < SEMC1 && SEMC1 > UBC1 && SEML1 > UBC1 && SEMC1 > UBC1 && MAL3 > UBC1 && (SEML1 > SEMC1 || SEML2 > SEMC1))
            {
                cParam.SideCounter = 7.1795M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_7_2999(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal SEMC8 = (decimal?)cStudy[FieldKeys.StudySEM__C8] ?? 0.0M;
        {
            if (cHigh >= Math.Min(SEMC5, SEMC8))
            {
                cParam.SideCounter = 7.2999M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_7_3598(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        {
            if (cHigh < SEML1 && SEML1 > UBC1)
            {
                cParam.SideCounter = 7.3598M;
                return true;
            }
        }
    
        return false;
    }

    private bool StopRule_7_9992(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }
    
        decimal cHigh = cStudy.Price.High;
        decimal C5_SEMTrend = (decimal?)cStudy[FieldKeys.StudyCycleTrendEx__C5_SEMTrend] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        if (C5_SEMTrend == -0.1M && SEMC1 > UBC1 && cHigh < SEMC1)
        {
            cParam.SideCounter = 7.9992M;
            return true;
        }
    
        return false;
    }
}
