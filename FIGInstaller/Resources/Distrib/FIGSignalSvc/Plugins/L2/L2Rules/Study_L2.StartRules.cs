using FIGCommon.Utilities;

namespace FIG.Studies;

public partial class L2RuntimeRules : L2RuleScript
{

    public override bool MeetsDefaultStartCondition(CircularBuffer<BarStudy> studies, Study_L2Params parameters, bool requireZeroSideCounter)
    {
        if (paramHist.Items[1] is not Study_L2Params || studies.Items[0] is not { } current || studies.Items[1] is null)
            return false;
        return parameters.Side == 0
            && (!requireZeroSideCounter || parameters.SideCounter == 0)
            && current.Price.Low > ((decimal?)current[FieldKeys.StudyMA__L2] ?? 0M);
    }
    private bool StartRule_1_9445(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_1_9447(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_1_9448(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal UBC2 = (decimal?)cStudy[FieldKeys.StudyBB__C2_1_UB] ?? 0.0M;
        decimal UBC3 = (decimal?)cStudy[FieldKeys.StudyBB__C3_1_UB] ?? 0.0M;
        return cHigh > Math.Max(Math.Max(SEMC3, UBC3), UBC2);
    }

    private bool StartRule_1_9499(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && cLow > MAL3;
    }

    private bool StartRule_1_9846(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        return (cHigh > Math.Max(SEMC2, SEMC3) || cHigh >= SEMC5);
    }

    private bool StartRule_4_0145(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_4_0146(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_4_0197(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_4_0247(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= SEMC5;
    }

    private bool StartRule_4_0248(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= SEMC5;
    }

    private bool StartRule_4_0548(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cClose > UBC1;
    }

    private bool StartRule_4_0699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cClose > SEMC1;
    }

    private bool StartRule_4_0898(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC4 = (decimal?)cStudy[FieldKeys.StudySEM__C4] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (SEMC2 > SEMC4)
        {
            if (cLow > SEML1 && cLow > SEML2)
            {
                return true;
            }
        }

        if (SEMC2 <= SEMC4)
        {
            if (cLow > SEMC2 && cLow > SEML1 && cLow > SEML2)
            {
                return true;
            }
        }

        return false;
    }

    private bool StartRule_4_0899(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEMC1 && cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_4_2799(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && cLow > MAL3;
    }

    private bool StartRule_4_2899(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1 && cLow > MAL1;
    }

    private bool StartRule_4_2998(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_3291(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1;
    }

    private bool StartRule_4_3292(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_3294(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1;
    }

    private bool StartRule_4_3295(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return (SEML2 < MAL2 || SEML1 < MAL2 || MAL3 < MAL2);
    }

    private bool StartRule_4_3296(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (SEML1 < SEMC1 && SEML2 < SEMC1)
        {
            if (cLow > SEML1 && cLow > SEML2)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_4_3297(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (SEML1 < SEMC1 && SEML2 < SEMC1)
        {
            if (cLow > SEML1 && cLow > SEML2)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_4_3298(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        return cLow > MAL1;
    }

    private bool StartRule_4_3349(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC3 = (decimal?)cStudy[FieldKeys.StudyMA__C3] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1 && cHigh < MAC3;
    }

    private bool StartRule_4_3399(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return (SEML2 < MAL1 || SEML1 < MAL1);
    }

    private bool StartRule_4_3494(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        decimal MAC5 = (decimal?)cStudy[FieldKeys.StudyMA__C5] ?? 0.0M;
        if (MAC1 < MAC5)
        {
            if (cLow > MAC1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_4_3496(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1;
    }

    private bool StartRule_4_3499(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1;
    }

    private bool StartRule_4_3598(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > MAL3 && cLow > SEML2 && cLow > SEML1;
    }

    private bool StartRule_4_3749(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cClose >= UBC1;
    }

    private bool StartRule_4_4797(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1 && cLow > MAL3;
    }

    private bool StartRule_4_4798(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1 && cLow > MAL3;
    }

    private bool StartRule_4_5496(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEMC4 = (decimal?)cStudy[FieldKeys.StudySEM__C4] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        return SEMC4 > SEMC5;
    }

    private bool StartRule_4_5499(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEMC4 = (decimal?)cStudy[FieldKeys.StudySEM__C4] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        return SEMC4 > SEMC5;
    }

    private bool StartRule_4_5648(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return UBC1 > SEMC3 && cHigh >= UBC1;
    }

    private bool StartRule_4_5698(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1 && cLow > MAL3;
    }

    private bool StartRule_4_5699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1 && cLow > MAL3;
    }

    private bool StartRule_4_5749(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_4_5993(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_5995(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL3;
    }

    private bool StartRule_4_5998(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL3;
    }

    private bool StartRule_4_6592(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        return cLow > MAL1;
    }

    private bool StartRule_4_6593(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_6595(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_6597(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_6598(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_6897_Negative(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        if (cParam.SideCounter == 0)
        {
            return false;
        }

        if (cParam.SideCounter == 4.6897M && SEML1 > UBC1 && cHigh < SEML1)
        {
            cParam.SideCounter = 0;
            return true;
        }

        return false;
    }

    private bool StartRule_4_6948(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cLow > UBC1;
    }

    private bool StartRule_4_7197(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal cLow = cStudy.Price.Low;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        return cLow > SEMC3 && cLow > SEMC2 && cClose > SEMC1;
    }

    private bool StartRule_4_7199(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cLow > MAL3 && cLow > UBC1;
    }

    private bool StartRule_4_7247(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= Math.Max(SEMC1, UBC1);
    }

    private bool StartRule_4_7446_Positive(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC3 = (decimal?)cStudy[FieldKeys.StudyMA__C3] ?? 0.0M;
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (SEML2 < MAC3 && SEML2 < MAL2 && MAL3 < MAL2)
        {
            return true;
        }
        else if (SEML2 > MAC3 && cHigh < SEML2)
        {
            return false;
        }

        return false;
    }

    private bool StartRule_4_7446_Negative(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC3 = (decimal?)cStudy[FieldKeys.StudyMA__C3] ?? 0.0M;
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (SEML2 < MAC3 && SEML2 < MAL2 && MAL3 < MAL2)
        {
            return false;
        }
        else if (SEML2 > MAC3 && cHigh < SEML2)
        {
            return true;
        }

        return false;
    }

    private bool StartRule_4_7447(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        decimal LBC4 = (decimal?)cStudy[FieldKeys.StudyBB__C4_1_LB] ?? 0.0M;
        return cHigh >= Math.Min(UBC1, LBC4);
    }

    private bool StartRule_4_7749(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal LBC3 = (decimal?)cStudy[FieldKeys.StudyBB__C3_1_LB] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= Math.Min(UBC1, LBC3);
    }

    private bool StartRule_4_7849(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal LBC3 = (decimal?)cStudy[FieldKeys.StudyBB__C3_1_LB] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= Math.Min(UBC1, LBC3);
    }

    private bool StartRule_4_8296(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal LB2C4 = (decimal?)cStudy[FieldKeys.StudyBB__C4_2_LB] ?? 0.0M;
        decimal LB2C5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_2_LB] ?? 0.0M;
        decimal LBC2 = (decimal?)cStudy[FieldKeys.StudyBB__C2_1_LB] ?? 0.0M;
        return cLow > LB2C5 && cLow > LB2C4 && cLow > LBC2;
    }

    private bool StartRule_4_8698(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal LB2C4 = (decimal?)cStudy[FieldKeys.StudyBB__C4_2_LB] ?? 0.0M;
        decimal LB2C3 = (decimal?)cStudy[FieldKeys.StudyBB__C3_2_LB] ?? 0.0M;
        return cLow > LB2C3 && cLow > LB2C4;
    }

    private bool StartRule_4_8699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_4_8897(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL3;
    }

    private bool StartRule_4_8898(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL3;
    }

    private bool StartRule_4_8899(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL3;
    }

    private bool StartRule_4_9449(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        return cClose > MAC1 && cClose > MAC2;
    }

    private bool StartRule_4_9835(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh > UBC1;
    }

    private bool StartRule_4_9893(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        return ((cLow > SEMC3) || (MAL3 > MAL1));
    }

    private bool StartRule_4_9895(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return ((SEMC1 < UBC1 && cLow > MAL3) || (SEMC1 > UBC1 && cLow > MAL3 && cLow > SEMC1));
    }

    private bool StartRule_6_4448(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cHigh >= SEMC2;
    }

    private bool StartRule_6_4548(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UB2C1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_2_UB] ?? 0.0M;
        return cHigh >= UB2C1;
    }

    private bool StartRule_6_4549(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= SEMC1;
    }

    private bool StartRule_6_4747(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_6_5149(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UB2C1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_2_UB] ?? 0.0M;
        return cHigh >= UB2C1;
    }

    private bool StartRule_6_6447(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cHigh < SEML2 && SEML2 > MAL2;
    }

    private bool StartRule_6_6448(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= SEMC1;
    }

    private bool StartRule_6_6694(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        return MAL2 > SEMC2 && cLow > SEMC3;
    }

    private bool StartRule_6_6697(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC2 = (decimal?)cStudy[FieldKeys.StudyMA__C2] ?? 0.0M;
        return MAC2 > MAC3;
    }

    private bool StartRule_6_6699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_6_7396(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_6_7495(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1;
    }

    private bool StartRule_7_0596(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC8 = (decimal?)cStudy[FieldKeys.StudySEM__C8] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return SEMC1 > SEMC8 && cLow > SEML1 && cLow > SEML2 && cLow > MAL3;
    }

    private bool StartRule_7_0697(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_1299(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        if (SEML1 > MAL2 || SEML1 > MAL2)
        {
            if (cLow > SEMC2)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_7_1495(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && cLow > MAL3;
    }

    private bool StartRule_7_1549(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC8 = (decimal?)cStudy[FieldKeys.StudyBB__C8_1_UB] ?? 0.0M;
        return cHigh >= Math.Min(SEMC5, UBC8);
    }

    private bool StartRule_7_1649(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return SEMC5 < SEMC8 && cHigh >= SEMC5;
    }

    private bool StartRule_7_1699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1;
    }

    private bool StartRule_7_1741(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_1947(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return SEML1 > UBC1 && cHigh < SEML1;
    }

    private bool StartRule_7_1948(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_1998(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return (cLow > MAL1 || SEML2 > MAL2 || SEML1 > MAL2 || MAL3 > MAL2);
    }

    private bool StartRule_7_2244(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal cClose = cStudy.Price.Close;
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cClose > UBC1;
    }

    private bool StartRule_7_2397(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        return cLow > SEMC1;
    }

    private bool StartRule_7_2398(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return cLow > SEMC2;
    }

    private bool StartRule_7_2699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_2948(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        return cHigh >= SEMC3;
    }

    private bool StartRule_7_2999(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        return cLow > MAL1 && cLow > MAL3;
    }

    private bool StartRule_7_3435(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_3436(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_3437(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_3439(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_3496(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && cLow > MAL3;
    }

    private bool StartRule_7_3598(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        return cLow > MAC1;
    }

    private bool StartRule_7_3599(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        return (cLow > SEMC2 || cLow > MAL1);
    }

    private bool StartRule_7_3648(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= Math.Min(SEMC5, SEMC8);
    }

    private bool StartRule_7_4247(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC8 = (decimal?)cStudy[FieldKeys.StudySEM__C8] ?? 0.0M;
        return cHigh >= SEMC8;
    }

    private bool StartRule_7_4248(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC13 = (decimal?)cStudy[FieldKeys.StudySEM__C13] ?? 0.0M;
        return cHigh >= SEMC13;
    }

    private bool StartRule_7_4448(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_4545(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        return SEMC1 > LBC1;
    }

    private bool StartRule_7_4547(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        return cHigh >= SEMC5 && SEMC1 > LBC1;
    }

    private bool StartRule_7_4698(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        return cLow > MAL1;
    }

    private bool StartRule_7_4699(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        return cLow > MAL1;
    }

    private bool StartRule_7_4798(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && cLow > MAL1 && cLow > MAL3;
    }

    private bool StartRule_7_4997(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_5399(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML2 && cLow > SEML1;
    }

    private bool StartRule_7_5497(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_5498(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2 && (cLow > SEMC3 || cLow > SEMC5);
    }

    private bool StartRule_7_5499(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_5598(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_5599(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_6148(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_6199(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > MAL3 && cLow > SEMC1 && cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_6395(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        return cLow > MAL1;
    }

    private bool StartRule_7_6399(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal UB2C5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_2_UB] ?? 0.0M;
        if (SEMC1 > UB2C5 && SEMC2 > UB2C5 && SEMC1 < SEMC2)
        {
            if (cLow > SEMC1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_7_6547(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh > UBC1;
    }

    private bool StartRule_7_6548(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh < SEML2 && SEML2 > UBC1;
    }

    private bool StartRule_7_6592(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        if (SEMC1 < SEMC2)
        {
            if (cLow > SEMC1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_7_6593(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        if (SEMC1 < SEMC2)
        {
            if (cLow > SEMC1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_7_6597(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        return (SEMC2 > SEMC5 || SEMC3 > SEMC5);
    }

    private bool StartRule_7_6599(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEMC2 = (decimal?)cStudy[FieldKeys.StudySEM__C2] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal LBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_LB] ?? 0.0M;
        if (SEMC1 < LBC5 && SEMC2 < LBC5 && SEMC3 < LBC5 && SEMC5 < LBC5)
        {
            if (cLow > SEMC2 && cLow > SEMC1)
            {
                return true;
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private bool StartRule_7_7499(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal SEMC5 = (decimal?)cStudy[FieldKeys.StudySEM__C5] ?? 0.0M;
        decimal LB2C8 = (decimal?)cStudy[FieldKeys.StudyBB__C8_2_LB] ?? 0.0M;
        decimal LB2C13 = (decimal?)cStudy[FieldKeys.StudyBB__C13_2_LB] ?? 0.0M;
        if (MAL1 > LB2C8 || MAL1 > LB2C13)
        {
            if (cLow > SEMC5)
            {
                return true;
            }
        }

        return MAL1 < LB2C8 && MAL1 < LB2C13;
    }

    private bool StartRule_7_7692(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_7798(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return cLow > SEML1 && cLow > SEML2;
    }

    private bool StartRule_7_8992(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        return (SEML2 > SEML1 || SEML2 > LBC1);
    }

    private bool StartRule_7_8994(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return SEML2 > SEML1;
    }

    private bool StartRule_7_8995(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        if (MAL2 >= SEMC1)
        {
            return true;
        }
        else if (MAL2 < SEMC1)
        {
            if (SEML2 > MAL2 || MAL3 > MAL2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }

    private bool StartRule_7_8996(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal SEML1 = (decimal?)cStudy[FieldKeys.StudySEM__L1] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        return (SEML2 > SEML1 || SEML2 > LBC1);
    }

    private bool StartRule_7_8998(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal SEMC3 = (decimal?)cStudy[FieldKeys.StudySEM__C3] ?? 0.0M;
        return (cLow > MAC1 || cLow > MAL1 || cLow > SEMC3);
    }

    private bool StartRule_7_8999(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal MAC1 = (decimal?)cStudy[FieldKeys.StudyMA__C1] ?? 0.0M;
        decimal MAL1 = (decimal?)cStudy[FieldKeys.StudyMA__L1] ?? 0.0M;
        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEMC1 = (decimal?)cStudy[FieldKeys.StudySEM__C1] ?? 0.0M;
        decimal UBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_UB] ?? 0.0M;
        if ((MAL3 < UBC5 || MAL3 < MAL2))
        {
            if (MAL2 >= SEMC1)
            {
                return true;
            }
            else if (MAL2 < SEMC1)
            {
                if (cLow > MAL1 || cLow > MAC1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        return false;
    }

    private bool StartRule_7_9197(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal MAL2 = (decimal?)cStudy[FieldKeys.StudyMA__L2] ?? 0.0M;
        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal SEML2 = (decimal?)cStudy[FieldKeys.StudySEM__L2] ?? 0.0M;
        return (MAL3 < MAL2 || SEML2 < MAL2);
    }

    private bool StartRule_7_9349(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        return cHigh >= MAC2;
    }

    private bool StartRule_7_9599(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
    {
        var cParam = candidateParameters ?? (Study_L2Params?)paramHist.Items[0];
        var pParam = (Study_L2Params?)paramHist.Items[1];
        BarStudy? cStudy = studies.Items[0];
        BarStudy? pStudy = studies.Items[1];
        if (cParam == null || pParam == null || cStudy == null || pStudy == null)
        {
            return false;
        }

        decimal MAL3 = (decimal?)cStudy[FieldKeys.StudyMA__L3] ?? 0.0M;
        decimal LBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_LB] ?? 0.0M;
        int C1_SEMShortTrend = (int?)cStudy[FieldKeys.StudyCycleTrendEx__C1_SEMShortTrend] ?? 0;
        return (MAL3 < LBC1 || C1_SEMShortTrend != 0);
    }

    private bool StartRule_7_9942(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UBC1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_1_UB] ?? 0.0M;
        return cHigh >= UBC1;
    }

    private bool StartRule_7_9945(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal UB2C1 = (decimal?)cStudy[FieldKeys.StudyBB__C1_2_UB] ?? 0.0M;
        decimal UB2C2 = (decimal?)cStudy[FieldKeys.StudyBB__C2_2_UB] ?? 0.0M;
        decimal UBC5 = (decimal?)cStudy[FieldKeys.StudyBB__C5_1_UB] ?? 0.0M;
        return (cHigh >= UB2C1 || cHigh >= UB2C2 || cHigh >= UBC5);
    }

    private bool StartRule_7_9996(CircularBuffer<BarStudy> studies, Study_L2Params candidateParameters)
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
        decimal LBC2 = (decimal?)cStudy[FieldKeys.StudyBB__C2_1_LB] ?? 0.0M;
        return cLow > LBC2;
    }
}



