this.basicTriggers = [];
this.matchingIds = [];

this.checkSignal =
    this.basicTrigger_DL1 === 0 &&
    cLow > this.L2.MA &&
    this.Side === 0;

if (!this.checkSignal) return;

this.basicTriggers.push({
    cond: ( // 6.66 done 
		this.C5.SEM > this.C21.UB1 && this.C5.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C5.SEM < this.C5.MA && this.C8.SEM > this.C8.MA && this.C13.SEM > this.C13.MA
		&& !( this.C8.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C8.SEM < this.C8.MA && this.C13.SEM > this.C13.MA) 
    ),
    id: 6.66
});
		
this.basicTriggers.push({
    cond: ( // 7.16 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 
		&& this.C1.SEM > this.C1.MA && this.C1.UB1 < this.C5.MA 
		&& ( (this.C5.SEM > this.C8.SEM) || (this.L2.MA > this.C8.SEM) ) 
    ),
    id: 7.16
});

this.basicTriggers.push({
    cond: ( // 7.29 Done
		this.C3.MA > this.C8.UB1 && this.L2.MA < this.C3.LB1
    ),
    id: 7.29
});

this.basicTriggers.push({
    cond: ( // 7.26 done
		this.C2.SEM > this.C5.UB2 && this.C1.SEM > this.C5.UB2
		&& this.C1.SEM < this.C2.SEM
		&& this.C4.SEM > this.C4.UB1 && this.C5.SEM > this.C5.UB1 && this.C8.SEM > this.C8.UB1
    ),
    id: 7.26
});

this.basicTriggers.push({
    cond: ( // 7.24 Done
		this.C8.SEM < this.C5.LB1 && this.C13.SEM < this.C8.LB1 && this.C21.SEM < this.C13.LB1
		&& this.C34.SEM < this.C21.LB1 
		&& this.C8.SEM < this.C13.SEM && this.C13.SEM < this.C21.SEM && this.C21.SEM < this.C34.SEM
		&& this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2
		&& this.L1.MA < this.C13.LB2 && this.L1.MA < this.C21.LB2
		&& this.L2.MA < this.C13.LB2 && this.L2.MA < this.C21.LB2
    ),
    id: 7.24
});

this.basicTriggers.push({
    cond: ( // 7.48 done 
		this.C21.SEM < this.C21.LB1 && this.C13.SEM > this.C13.LB1 && this.C13.SEM < this.C13.MA 
		&& this.C13.SEM < this.C21.SEM
    ),
    id: 7.48
});
		
this.basicTriggers.push({
    cond: ( // 7.45 Done 
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C21_SEMLB1LB2_Stage === -1
    ),
    id: 7.45
});

this.basicTriggers.push({
    cond: ( // 7.42 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1 && this.C5.SEM < this.C5.LB1 
		&& this.C13.SEM < this.C8.LB1 && this.C8.SEM < this.C5.LB1
		&& this.L2.MA < this.C8.SEM && this.L2.MA < this.C13.SEM
		&& this.L2.MA > this.C5.LB2 
    ),
    id: 7.42
});

this.basicTriggers.push({
    cond: ( // 7.43 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1 && this.C5.SEM < this.C5.MA && this.C5.SEM < this.C21.SEM 
		&& this.C21.SEM < this.C5.MA
    ),
    id: 7.43
});

this.basicTriggers.push({
    cond: ( // 7.47 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA
		&& this.L2.MA > this.C21.LB2
    ),
    id: 7.47
});

this.basicTriggers.push({
    cond: ( // 4.92 done				
		(cStudy.C2_C8_L_Cont_UB1 === 1 && cStudy.C3_C8_L_Cont_UB1 === 1) 
		&& this.C3.SEM > this.C5.SEM && (this.L2.MA > this.C1.MA || this.C1.SEM > this.C1.MA) 
    ),
    id: 4.92
});

this.basicTriggers.push({
    cond: ( //  7.52 Done
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA < this.C89.MA 
    ),
    id: 7.52
});

this.basicTriggers.push({
    cond: ( //  7.51 Done
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA
		&& this.C8.SEM < this.C8.LB1 && this.C1.MA > this.C21.LB2
		&& this.C8.SEM < this.C13.LB2 && this.C5.SEM < this.C13.LB2
    ),
    id: 7.51
});

this.basicTriggers.push({
    cond: ( // 7.49 Done 
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA
		&& this.C8.SEM < this.C8.LB1 && this.C1.MA > this.C21.LB2
		&& this.C5.SEM > this.C8.SEM
    ),
    id: 7.49
});

this.basicTriggers.push({
    cond: ( // 7.55 Done
		this.C2.MA < this.C13.LB1 && this.C8.SEM < this.C8.MA && this.C8.SEM > this.C8.LB1
		&& this.C13.SEM < this.C13.MA && this.C5.SEM < this.C5.LB1
		&& this.C21.SEM < this.C21.UB1 && this.C21.SEM > this.C21.MA
    ),
    id: 7.55
});

this.basicTriggers.push({
    cond: ( // 7.54 Done
		this.C2.MA < this.C13.LB1 && this.C8.SEM < this.C8.MA && this.C8.SEM > this.C8.LB1
		&& this.C13.SEM < this.C13.MA && this.C5.SEM < this.C5.LB1
		&& this.C21.SEM < this.C21.MA
		&& this.C21.UBLB2_Dir === 1 && this.C21.UBLB1_Dir === 1
    ),
    id: 7.54
});
	
this.basicTriggers.push({
    cond: ( // 7.69 Done
		this.C3.SEM > this.C5.UB2 && this.C2.SEM > this.C5.UB2 && this.C1.SEM > this.C5.UB2
		&& this.C5.SEM > this.C5.UB1
    ),
    id: 7.69
});

this.basicTriggers.push({
    cond: ( // 7.59 Done 
		this.C13.SEM < this.C13.MA
		&& this.C5.SEM < this.C5.UB1 && this.C8.SEM < this.C8.UB1 
		&& (this.L2.MA > this.C13.UB2 || this.L1.MA > this.C13.UB2 || this.L3.MA > this.C13.UB2) 
    ),
    id: 7.59
});

this.basicTriggers.push({
    cond: ( //  7.62 Done
		this.L2.MA < this.C21.MA && this.C13.SEM < this.C13.MA && this.C21.SEM > this.C21.UB1
		&& this.C13.UBLB1_Dir === 1 && this.C13.UBLB2_Dir === -1
    ),
    id: 7.62
});

this.basicTriggers.push({
    cond: ( // 7.58 done
		this.C13.SEM < this.C13.LB1 && this.C21.SEM > this.C21.UB1 
    ),
    id: 7.58
});

this.basicTriggers.push({
    cond: ( // 7.61 done
		this.L2.MA < this.C21.MA && this.C13.SEM < this.C13.MA 
		&& this.C13.UBLB1_Dir === 1 && this.C13.UBLB2_Dir === -1
    ),
    id: 7.61
});

this.basicTriggers.push({
    cond: ( //  7.74 Done
		this.C21.SEM > this.C21.UB1  
		&& this.L2.MA < this.C8.LB2 && this.L2.MA < this.C13.LB2
		&& this.C1.MA > this.C13.LB2 && this.C1.MA > this.C8.LB2
    ),
    id: 7.74
});

this.basicTriggers.push({
    cond: ( // 4.02 Done 
		this.C5.SEM < this.C5.MA && this.C5.LB2 > this.C8.MA && this.L2.MA < this.C5.SEM
		&& this.C5.MA > this.C8.MA
    ),
    id: 4.02
});

this.basicTriggers.push({
    cond: ( //  4.14 done need revision 4.1419 should short but risky
		this.C8.SEM > this.C8.UB1 && this.C5.SEM < this.C5.LB1
    ),
    id: 4.14
});

this.basicTriggers.push({
    cond: ( // 4.01 Done
		this.C8.SEM > this.C8.UB1 
		&& this.C1.SEM < this.C8.UB1 && this.C2.SEM < this.C8.UB1 && this.C3.SEM < this.C8.UB1 
		&& this.C4.SEM < this.C8.UB1 && this.C5.SEM < this.C8.UB1 && this.C13.SEM < this.C8.UB1 && this.C21.SEM < this.C8.UB1
    ),
    id: 4.01
});

this.basicTriggers.push({
    cond: ( // 7.15 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 
		&& this.C1.SEM > this.C1.MA && this.C1.UB1 < this.C5.MA 
    ),
    id: 7.15
});
	
this.basicTriggers.push({
    cond: ( // 4.05 Done 
		this.C5.SEM > this.C5.UB1 && this.C3.SEM < this.C3.LB1
    ),
    id: 4.05
});

this.basicTriggers.push({
    cond: ( // 7.36 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C5.UB1 && this.C2.SEM < this.C5.UB1 && this.C3.SEM < this.C5.UB1 
		&& this.C4.SEM < this.C5.UB1 && this.C13.SEM < this.C5.UB1 && this.C21.SEM < this.C5.UB1
		&& this.C8.SEM < this.C5.UB1
    ),
    id: 7.36
});

this.basicTriggers.push({
    cond: ( // 7.35 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C5.UB1 && this.C2.SEM < this.C5.UB1 && this.C3.SEM < this.C5.UB1 
		&& this.C4.SEM < this.C5.UB1 && this.C13.SEM < this.C5.UB1 && this.C21.SEM < this.C5.UB1
		&& this.C8.SEM > this.C5.UB1
    ),
    id: 7.35
});

this.basicTriggers.push({
    cond: ( // 4.08 done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM < this.C8.LB1
    ),
    id: 4.08
});

this.basicTriggers.push({
    cond: ( // 4.07 Done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.UB1
		&& this.C8.SEM < this.C13.SEM && this.C5.SEM < this.C5.MA
    ),
    id: 4.07
});

this.basicTriggers.push({
    cond: (  // 4.34 Done
		this.C21.SEM < this.C21.UB1 && this.C21.SEM < this.C5.UB2 && this.C21.SEM > this.C5.UB1
		&& this.L2.MA < this.C21.SEM && this.C13.SEM < this.C21.SEM && this.C8.SEM < this.C21.SEM
		&& this.C5.SEM < this.C21.SEM && this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA
		&& this.C13.SEM < this.C13.MA && this.C21.SEM > this.C21.MA
    ),
    id: 4.34
});

this.basicTriggers.push({
    cond: (  // 4.36 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.C21.SEM > this.C21.MA && this.C13.SEM > this.C13.LB1 
		&& this.C8.SEM < this.C5.LB1 && this.C8.SEM < this.C2.MA
		&& this.C5.SEM < this.C3.LB1
		&& this.L2.MA > this.C89.MA
    ),
    id: 4.36
});

this.basicTriggers.push({
    cond: (  // 4.48 Done
		this.C5.MA > this.C89.MA && this.C13.MA > this.C21.MA && this.C21.SEM > this.C21.MA
		&& this.C3.MA < this.C13.LB1 && this.C13.SEM < this.C13.LB1 && this.C13.SEM < this.C3.MA
    ),
    id: 4.48
});

this.basicTriggers.push({
    cond: ( // 4.43 Done
		this.C2.MA > this.C3.MA && this.C3.MA > this.C5.MA && this.C5.MA < this.C13.MA
		&& this.C5.SEM > this.C5.MA && this.C8.SEM < this.C8.MA && this.C13.SEM < this.C13.LB1
		&& this.C5.SEM > this.C13.SEM && this.C8.SEM > this.C13.SEM
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.MA
		&& this.L2.MA > this.C2.SEM
    ),
    id: 4.43
});
	
this.basicTriggers.push({
    cond: ( // 4.47 Done
		this.C1.SEM < this.C2.LB2 && this.C2.SEM < this.C3.LB2
    ),
    id: 4.47
});

this.basicTriggers.push({
    cond: (  // 4.52 done  
		this.C5.SEM < this.C5.LB1 && this.C1.UB1 < this.C5.SEM && this.C1.UB1 < this.C3.SEM
    ),
    id: 4.52
});

this.basicTriggers.push({
    cond: ( // 4.75 Done
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& this.C2.MA > this.C3.MA
    ),
    id: 4.75
});

this.basicTriggers.push({
    cond: ( // 4.71 Done 
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& this.C1.UB1 < this.C4.LB1
    ),
    id: 4.71
});

this.basicTriggers.push({
    cond: (  // 4.67 Done
		this.C2.MA < this.C13.LB2 && this.C2.MA < this.C5.LB1
    ),
    id: 4.67
});

this.basicTriggers.push({
    cond: ( // 4.74 done
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& (this.C1.MA > this.C3.MA || this.C1.MA > this.C2.MA) 
    ),
    id: 4.74
});

this.basicTriggers.push({
    cond: ( // 4.73 Done
		this.C2.MA < this.C21.LB2 && this.C3.MA < this.C21.LB2
    ),
    id: 4.73
});

this.basicTriggers.push({
    cond: ( // 4.99 done  
		this.C1.SEM < this.C1.MA && this.C1.SEM < this.C5.LB2 
		&& (this.L2.MA < this.C3.SEM || this.L2.MA < this.C4.SEM) && this.L2.MA < this.C1.MA
		&& this.C3.SEM < this.C3.MA && this.C5.SEM < this.C5.MA
		&& this.C1.MA > this.C2.SEM && this.C1.MA > this.C3.SEM && this.C1.MA > this.C4.SEM && this.C1.MA > this.C5.SEM
		&& this.L2.MA > this.C2.SEM 
    ),
    id: 4.99
});

this.basicTriggers.push({
    cond: ( // 4.87 done
		this.L1.MA < this.C5.LB2 && this.L2.MA < this.C4.LB2
		&& this.L3.MA < this.C2.LB2
    ),
    id: 4.87
});

this.basicTriggers.push({
    cond: ( // 7.13 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1  
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C5.SEM < this.C8.SEM
		&& cStudy.C8_SEMTrend !== -0.1 && cStudy.C5_SEMTrend !== -0.1
    ),
    id: 7.13
});

this.basicTriggers.push({
    cond: ( // 4.85 Done 
		this.L1.MA > this.C5.LB2 && this.L2.MA > this.C4.LB2
		&& this.L3.MA < this.C2.LB2
    ),
    id: 4.85
});

this.basicTriggers.push({
    cond: ( // 7.19 Done
		this.C5.SEM < this.C5.MA  
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1
    ),
    id: 7.19
});

this.basicTriggers.push({
    cond: ( // 7.93 done
		this.L2.MA < this.C13.LB2 && this.C8.MA > this.C13.MA
    ),
    id: 7.93
});

this.basicTriggers.push({
    cond: (  // 4.88 done
		this.C2.SEM < this.C4.LB2 && this.C1.SEM > this.C1.MA 
    ),
    id: 4.88
});

this.basicTriggers.push({
    cond: (  // 4.86 done
		this.L2.MA < this.C3.LB2  
    ),
    id: 4.86
});

this.basicTriggers.push({
    cond: (  // 4.82 done
		this.C1.SEM < this.C2.LB2 && this.C2.SEM < this.C4.LB2 && this.C3.SEM > this.C5.LB2
    ),
    id: 4.82
});

this.basicTriggers.push({
    cond: (  // 4.83 done
		this.C4.SEM < this.C2.LB1 && this.C2.SEM < this.C5.LB2 && this.C3.SEM > this.C5.LB2
    ),
    id: 4.83
});

this.basicTriggers.push({
    cond: (  // 4.84 done
		this.C2.MA < this.C5.MA && this.C1.MA > this.C4.SEM && this.C1.UB1 < this.C5.SEM
		&& this.C2.SEM < this.C4.LB2 
		&& this.L2.MA > this.C8.LB2 && this.L2.MA > this.C5.LB2
    ),
    id: 4.84
});
	
this.basicTriggers.push({
    cond: (  // 4.94 done
		this.C1.MA > this.C3.SEM && this.C1.MA > this.C2.SEM 
		&& this.C2.MA > this.C13.LB1 && this.C5.SEM > this.C8.LB2
		&& (pStudy.C2_C5_LLB2_Cont_MAC === 1 || cStudy.C2_C5_LLB2_Cont_MAC === 1) 
    ),
    id: 4.94
});

this.basicTriggers.push({
    cond: (  // 4.91 done
		this.C2.MA > this.C13.LB1 
		&& (cStudy.C3_C5_LLB2_Cont_MAC === 1) 
    ),
    id: 4.91
});

this.basicTriggers.push({
    cond: ( // 4.93 done
		this.C3.SEM <= Math.min(this.C13.LB2, this.C21.LB2) 
		&& this.L2.MA > this.C5.LB2  
		&& this.C1.MA > this.C4.SEM && this.C1.MA > this.C5.SEM
		&& this.C2.SEM < this.C5.LB2 
    ),
    id: 4.93
});

this.basicTriggers.push({
    cond: ( // 4.98 done 
		this.C3.SEM < this.C5.LB2 
    ),
    id: 4.98
});

this.basicTriggers.push({
    cond: ( // 7.84 done  
		this.C1.LB2 > this.C3.UB1 
    ),
    id: 7.84
});

this.basicTriggers.push({
    cond: ( // 4.06 done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.UB1
		&& this.C8.SEM < this.C13.SEM && this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA
    ),
    id: 4.06
});

this.basicTriggers.push({
    cond: (  // 6.9 done
		this.L2.MAC_Pivot_Trend === -1 && this.C2.MAC_Pivot_Trend === -1 && this.C3.MAC_Pivot_Trend === -1 && this.C5.MAC_Pivot_Trend === 1 
		&& cStudy.C5_C3_MATrend === -1 && cStudy.C5_C2_MATrend === -1
		&& this.L2.MAC_SupRes > this.C5.MAC_SupRes && this.C2.MAC_SupRes > this.C5.MAC_SupRes
		&& (this.L2.MAC_SupRes > this.C2.MAC_SupRes || this.L2.MAC_SupRes > this.C3.MAC_SupRes) 
    ),
    id: 6.9
});

this.basicTriggers.push({
    cond: ( // 6.73 done
		this.C2.SEM > this.C5.UB2 && this.C3.SEM > this.C5.UB2
		&& this.C2.SEM > this.C1.UB2 && this.C3.SEM > this.C1.UB2
		/*&& this.C1.UB2 >= this.C5.UB2*/
    ),
    id: 6.73
});

this.basicTriggers.push({
    cond: ( // 6.77 Done
		this.C1.SEM > this.C5.SEM && this.C1.SEM < this.C1.MA
		&& this.C5.SEM > this.C5.UB1
    ),
    id: 6.77
});
		
this.basicTriggers.push({
    cond: ( // 6.76 Done
		this.C5.SEM < this.C8.SEM && this.C5.SEM < this.C13.SEM && this.C5.SEM < this.C21.SEM
		&& this.C5.SEM > this.C5.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM > this.C13.UB1 && this.C21.SEM > this.C21.UB1
    ),
    id: 6.76
});

this.basicTriggers.push({
    cond: ( // 6.75 Done
		this.C1.MA > this.C8.UB2 
    ),
    id: 6.75
});

this.basicTriggers.push({
    cond: ( // 7.99 done
		cStudy.C5_Pivot_Status_Odd > 0
    ),
    id: 7.99
});
	
this.basicTriggers.push({
    cond: (   // 1.99 done
		(cStudy.C1_C5_S_Cont_LB2 === -1 && cStudy.C1_C5_S_Cont_LB1 === -1) 
    ),
    id: 1.99
});

this.basicTriggers.push({
    cond: (  // 1.98 done 
		cStudy.C1_C5_S_Cont_LB2 === -1 
    ),
    id: 1.98
});

this.basicTriggers.push({
    cond: (  // 1.97  done need more maybe conditions on short  
		cStudy.L1_C2_S_Cont_LB2 === -1 && cStudy.L1_C2_S_Cont_LB1 === -1
    ),
    id: 1.97
});

this.basicTriggers.push({
    cond: (  // 1.97  done need more maybe conditions on short
		cStudy.L1_C2_S_Cont_LB2 === -1 && cStudy.L1_C3_S_Cont_LB2 === -1
    ),
    id: 1.97
});

this.basicTriggers.push({
    cond: (  // 1.96 done 
		cStudy.L1_C3_S_Cont_LB1 === -1
    ),
    id: 1.96
});

this.basicTriggers.push({
    cond: (  // 1.95 done 
		(cStudy.C2_C8_S_Cont_MAC === -1) 
    ),
    id: 1.95
});

this.basicTriggers.push({
    cond: ( // 1.94 done  
		cStudy.C3_C8_S_Cont_LB1 === -1 && cStudy.C3_C5_S_Cont_LB1 === -1 
		&& this.C1.MA < this.C2.MA 
    ),
    id: 1.94
});

this.basicTriggers.push({
    cond: ( // 7.05 done
		this.C5.SEM > this.C8.UB2 && this.C5.SEM > this.C13.UB2 && this.C5.SEM > this.C21.UB2 
    ),
    id: 7.05
});

this.basicTriggers.push({
    cond: ( // 7.01 Done
		this.C5.SEM > this.C8.UB2 && this.C1.SEM > this.C8.UB2 && this.C1.SEM > this.C1.MA 
		&& (this.C5.SEM < this.C13.UB2 || this.C5.SEM < this.C21.UB2) 
    ),
    id: 7.01
});

this.basicTriggers.push({
    cond: ( // 6.72 done
		this.C1.SEM > this.C1.UB1 
		&& this.C5.SEM >= this.C5.MA && this.C5.SEM < this.C5.UB1
		&& this.C1.UB1 < this.C5.SEM && this.C1.UB2 > this.C5.SEM
    ),
    id: 6.72
});

this.basicTriggers.push({
    cond: ( // 7.06 done
		cStudy.C5_SEMTrend !== -0.1 && this.C5.SEM > this.C8.SEM && this.C5.SEM > this.C13.SEM
		&& this.L2.MA > this.C5.SEM && this.L2.MA > this.C5.UB1
		&& this.C5.SEM > this.C5.UB1 /*&& this.C8.SEM < this.C8.UB1*/
    ),
    id: 7.06
});

this.basicTriggers.push({
    cond: (  // 4.29 done
		this.C8.SEM > this.C8.UB1 
		&& this.C2.MA < this.C8.UB1 && this.C1.MA < this.C8.UB1
		&& this.C3.SEM > this.C8.UB1 && this.C4.SEM > this.C8.UB1 && this.C5.SEM > this.C8.UB1
    ),
    id: 4.29
});

this.basicTriggers.push({
    cond: (  // 4.31 done
		this.C21.SEM < this.C21.LB1
		&& this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1
    ),
    id: 4.31
});

this.basicTriggers.push({
    cond: (  // 7.86 done  //best trade is in 1.98  
		this.C2.LB1 > this.C5.UB1 
    ),
    id: 7.86
});

this.basicTriggers.push({
    cond: (   // 7.89 done
		this.C1.MA > this.C5.SEM && this.L2.MA > this.C5.SEM
		&& this.C1.SEM < this.C1.MA && this.L2.MA < this.C1.MA
		&& this.C3.MA > this.C5.MA && this.C2.MA > this.C3.MA && this.C1.MA > this.C2.MA 
		&& this.C5.SEM > this.C5.MA && this.C3.SEM > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.L2.MA > this.C5.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C2.MA 
    ),
    id: 7.89
});

this.basicTriggers.push({
    cond: (  // 7.88 done 
		this.C1.MA > this.C5.SEM && this.L2.MA <= this.C5.SEM
		&& this.C1.SEM < this.C1.MA && this.L2.MA < this.C1.MA
		&& this.C3.MA > this.C5.MA && this.C2.MA > this.C3.MA && this.C1.MA > this.C2.MA 
		&& this.C5.SEM > this.C5.MA && this.C3.SEM > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.L2.MA > this.C5.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C2.MA 
    ),
    id: 7.88
});

this.basicTriggers.push({
    cond: ( // 7.91 done 
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 && cStudy.C5_SEMTrend !== -0.1 
		&& this.C1.SEM > this.C5.SEM && this.C1.SEM > this.C1.MA
		&& this.C4.SEM < this.C1.MA 
		&& this.L2.MA > this.C1.MA && this.L2.MA > this.C2.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C5.MA
		&& this.L2.MA > this.C5.SEM && this.L2.MA > this.C3.SEM
		&& this.C1.MA > this.C2.MA && this.C1.MA > this.C3.MA && this.C1.SEM > this.C3.SEM && this.C1.SEM > this.C4.SEM 
		&& this.C2.SEM > this.C3.SEM && this.C2.SEM > this.C5.SEM					
		&& this.C2.MA > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.C3.MA < this.C5.MA && this.C3.SEM > this.C5.SEM
    ),
    id: 7.91
});

this.basicTriggers.push({
    cond: ( // 6.74 Done 
		this.C4.SEM > this.C5.SEM 
		&& this.C1.MA > this.C5.UB1 && this.C1.MA > this.C4.UB1
		&& this.C4.SEM < this.C4.UB1 && this.C5.SEM < this.C5.UB1
		&& cStudy.C4_SEMTrend === 1 && cStudy.C5_SEMTrend === 1
    ),
    id: 6.74
});

this.basicTriggers.push({
    cond: ( // should go below 6.74 
		cStudy.C8_SEMTrend !== -0.1 && this.C8.SEM > this.C13.SEM 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C8.UB1
		&& this.C8.SEM > this.C8.UB1
    ),
    id: 6.79
});

this.basicTriggers.push({
    cond: (   // 7.94 done need more specific and block adjustment
		this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA 
		&& this.L2.MA < this.C2.SEM 
		/*&& cStudy.C3_SEMShortTrend !== 1*/ && cStudy.C2_SEMShortTrend !== 1
		&& this.L3.MA < this.C1.SEM && this.L3.MA < this.C1.LB1
		&& this.C2.SEM < this.C2.MA  
    ),
    id: 7.94
});

this.basicTriggers.push({
    cond: (   // 7.97 done need more specific
		this.L2.MA > this.C5.MA && this.L2.MA > this.C1.MA 
		&& this.C1.MA > this.C5.MA && this.C1.SEM > this.C1.MA && this.C1.SEM > this.L2.MA && this.C1.SEM < this.C8.SEM
		&& this.C2.MA < this.C5.MA && this.C2.SEM > this.C2.MA && this.C2.SEM < this.C8.SEM
		&& this.C3.MA < this.C5.MA && this.C3.SEM > this.C3.MA && this.C3.SEM < this.C8.SEM
		&& this.C5.SEM < this.C5.MA && this.C5.SEM < this.C8.SEM
    ),
    id: 7.97
});

this.basicTriggers.push({
    cond: ( //  6.64 Done
		this.C8.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C8.SEM < this.C8.MA && this.C13.SEM > this.C13.MA 
    ),
    id: 6.64
});
	
this.basicTriggers.push({
    cond: (   // 7.95 done need more specific && adding the flat position
		this.C2.SEM > this.C2.MA
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM 
    ),
    id: 7.95
});

this.basicTriggers.push({
    cond: (  // 7.92 done
		this.C2.SEM < this.C2.MA 
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM 
		&& this.L3.MA < this.C3.LB2 && this.C3.SEM > this.C5.SEM && this.C4.SEM > this.C5.SEM 
    ),
    id: 7.92
});
	
this.basicTriggers.push({
    cond: ( // 7.96 // done allshort maybe a stop loss or select when to sell
		this.C2.SEM < this.C2.MA	
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM 
		&& cStudy.C3_SEMShortTrend !== 1 && cStudy.C2_SEMShortTrend !== 1
		&& this.C2.MAC_Pivot_Trend === -1
		&& this.C2.SEM > this.C4.LB2 
		&& this.C2.SEM > this.C8.LB2
    ),
    id: 7.96
});
		
this.basicTriggers.push({
    cond: ( 	// 4.76 done
		this.L2.MA < this.C5.LB2 && this.C3.MA > this.C13.MA
		&& this.L2.MA >= this.C13.MA
		&& (this.L3.MA > this.C1.LB1 || this.L3.MA > this.C3.LB2)
		&& this.C3.SEM < this.C3.MA && this.L2.MA > this.C8.LB2
    ),
    id: 4.76
});

this.basicTriggers.push({
    cond: ( 	// 4.78 done
		this.L1.MA < this.C8.LB2 && this.L1.MA < this.C5.LB2
    ),
    id: 4.78
});

this.basicTriggers.push({
    cond: ( 	// 4.77 done 	
		this.L2.MA < this.C8.LB2 && this.L2.MA < this.C5.LB2
    ),
    id: 4.77
});

this.basicTriggers.push({
    cond: ( // 7.98 Done
		this.L2.MA < this.C5.LB2 && this.C3.MA > this.C13.MA
    ),
    id: 7.98
});

this.basicTriggers.push({
    cond: ( // 4.68 Done revised 2
		this.C5.SEM < this.C1.MA && this.C1.SEM < this.C1.UB1
		&& this.C2.MA < this.C5.LB1 && this.C8.SEM < this.C2.MA
		&& this.C2.SEM > this.C5.SEM && this.C2.SEM > this.C3.SEM
		&& this.C2.SEM < this.C2.MA	
    ),
    id: 4.68
});

this.basicTriggers.push({
    cond: ( // 4.96 done need to check short 42 41 39 
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 /*&& this.C5.SEM > this.C8.LB2*/
    ),
    id: 4.96
});

this.basicTriggers.push({
    cond: ( // 7.28 Done before 7.63 
		this.C8.SEM < this.C5.LB1 && this.C13.SEM < this.C8.LB1 && this.C21.SEM < this.C13.LB1
		&& this.C34.SEM < this.C21.LB1 
		&& this.C8.SEM < this.C13.SEM && this.C13.SEM < this.C21.SEM && this.C21.SEM < this.C34.SEM
    ),
    id: 7.28
});
	
this.basicTriggers.push({
    cond: ( //  7.63 Done
		this.C5.UB1 < this.C21.LB1 && this.C21.SEM < this.C21.LB1
    ),
    id: 7.63
});

this.basicTriggers.push({
    cond: ( // 4.69 Done
		this.C8.SEM > this.C13.SEM 
		&& (this.C8.SEM > this.C8.LB1 || this.C13.SEM > this.C8.LB1)
		&& this.C13.SEM < this.C13.LB1 && this.C13.SEM < this.C21.SEM
		&& this.C21.SEM < this.C21.MA
		&& this.C5.SEM < this.C5.LB1 && this.C5.SEM < this.C8.LB1
    ),
    id: 4.69
});

this.basicTriggers.push({
    cond: ( // 4.72 Done need second revision
		this.C5.MA < this.C89.LB1 && this.C13.SEM > this.C5.MA 
    ),
    id: 4.72
});

this.basicTriggers.push({
    cond: (  // 4.65 done need second revesion and short specs
		this.C13.SEM < this.C3.MA 
		&& this.C8.SEM < this.C2.MA && this.C5.SEM < this.C1.MA && this.C5.SEM < this.C3.LB1
    ),
    id: 4.65
});

this.basicTriggers.push({
    cond: (  // 4.57 done test diferent location and cycles 
		this.C5.SEM < this.C5.LB1 && this.C1.UB1 < this.C5.SEM 
    ),
    id: 4.57
});

this.basicTriggers.push({
    cond: (  // 4.56 done different locaion 
		this.C3.SEM < this.C3.MA && this.C1.UB1 < this.C3.SEM 
    ),
    id: 4.56
});

this.basicTriggers.push({
    cond: (  // 4.49 Done
		this.C2.MA > this.C3.MA && this.C3.MA > this.C5.MA && this.C1.MA > this.C5.MA
		&& this.C1.UB1 < this.C2.MA && this.C1.SEM > this.C1.MA
    ),
    id: 4.49
});

this.basicTriggers.push({
    cond: (  // 4.59 done 
		this.C5.MA > this.C89.MA && this.C13.MA > this.C89.MA && this.C21.MA > this.C89.MA
		&& this.C5.MA < this.C13.MA && this.C13.MA < this.C21.MA
		&& this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1
    ),
    id: 4.59
});
	
this.basicTriggers.push({
    cond: ( // 4.42 done 
		this.C5.SEM < this.C89.LB2 && this.C13.SEM < this.C89.LB2
		&& this.C5.SEM < this.C13.SEM && this.C5.SEM < this.C21.SEM && this.C8.SEM < this.C13.SEM
    ),
    id: 4.42
});
	
this.basicTriggers.push({
    cond: (  // 4.41 Done
		this.C2.MA < this.C3.MA && this.C3.MA < this.C5.MA && this.C1.MA < this.C5.MA
		&& this.C5.SEM < this.C5.LB1 && this.C8.SEM < this.C8.LB1 && this.C13.SEM < this.C13.LB1
		&& this.C1.UB1 < this.C2.MA /*&& this.C1.SEM > this.C1.MA*/
    ),
    id: 4.41
});

this.basicTriggers.push({
    cond: (  // 4.39 done need revision after seeing all SEMC13 < C13LB1 SEMC21 < C21LB1 
		this.L2.MA > this.C13.MA && this.C13.SEM < this.C13.LB1
    ),
    id: 4.39
});

this.basicTriggers.push({
    cond: (  // 4.54 done
		this.L2.MA > this.C3.MA && this.C2.SEM > this.C2.MA && this.C3.SEM > this.C3.MA 
		&& (this.C2.SEM > this.C3.SEM || this.C3.SEM > this.C4.SEM)
		&& this.C2.SEM > this.C5.SEM 
		&& this.C5.SEM > this.C8.SEM && this.C5.SEM > this.C13.SEM && this.C5.SEM > this.C21.SEM && this.C8.SEM > this.C13.SEM
		&& (this.L2.MA > this.C2.MA || this.L2.MA > this.C5.SEM) 
		&& this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1
    ),
    id: 4.54
});

this.basicTriggers.push({
    cond: (  // 4.37 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.L2.MA > this.C89.MA
		&& this.C13.SEM < this.C13.LB1 && this.C21.SEM > this.C21.MA
    ),
    id: 4.37
});

this.basicTriggers.push({
    cond: (  // 7.32 done
		this.C21.MA < this.C89.MA && this.C21.MA > this.C89.LB1 && this.C21.SEM < this.C21.MA
		&& this.C13.MA < this.C89.MA /*&& this.C13.MA > this.C89.LB1*/ && this.C13.SEM > this.C13.MA
		&& this.C89.SEM < this.C89.MA 
		&& this.C21.SEM < this.C89.SEM && this.C13.SEM > this.C21.SEM 
    ),
    id: 7.32
});

this.basicTriggers.push({
    cond: (  // 4.53 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.L2.MA > this.C89.MA
		&& this.C13.SEM < this.C13.MA && this.C8.SEM < this.C5.LB1 && this.C21.SEM > this.C21.MA
    ),
    id: 4.53
});

this.basicTriggers.push({
    cond: (  // 4.32 done
		this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.LB1
    ),
    id: 4.32
});

this.basicTriggers.push({
    cond: (  // 4.28 done 
		this.L2.MA > this.C21.MA && this.C21.SEM < this.C21.LB1
		&& this.C8.SEM < this.C8.UB1
    ),
    id: 4.28
});

this.basicTriggers.push({
    cond: (  // 4.27 Done
		this.C8.SEM > this.C8.UB1 && this.L2.MA > this.C8.UB1
		&& cStudy.C5_SEMTrend === 1.1 && cStudy.C4_SEMTrend === 1.1 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C5.SEM && this.L2.MA > this.C4.SEM
		&& this.C2.SEM > this.C3.SEM
    ),
    id: 4.27
});

this.basicTriggers.push({
    cond: (  // 4.25 done
		this.C8.SEM > this.C8.UB1 /*&& this.L2.MA > this.C8.UB1*/
		&& cStudy.C3_SEMTrend !== -0.1 && this.C3.SEM < this.C3.UB1 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C3.SEM && this.L2.MA > this.C5.SEM
    ),
    id: 4.25
});
	
this.basicTriggers.push({
    cond: (  // 4.19 done
		this.C8.SEM > this.C8.UB1 && this.L2.MA > this.C8.UB1
		&& cStudy.C3_C13_Flag_Trend_Lower === 1 && cStudy.C5_C21_Flag_Trend_Lower === -1
		&& this.C5.MA > this.C89.MA 
    ),
    id: 4.19
});

this.basicTriggers.push({
    cond: (  // 4.33 Done
		cStudy.C3_C13_Flag_Trend_Lower === 1 && cStudy.C5_C21_Flag_Trend_Lower === -1 
		&& this.C3.MA < this.C13.MA && this.C13.SEM < this.C13.UB1 
    ),
    id: 4.33
});

this.basicTriggers.push({
    cond: (  // 4.35 Done
		this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1
		&& this.L2.MA > this.C89.MA
    ),
    id: 4.35
});

this.basicTriggers.push({
    cond: ( // 7.57 Done
		this.C21.SEM > this.C21.UB1  
		&& this.L2.MA < this.C8.LB2 && this.L2.MA < this.C13.LB2
    ),
    id: 7.57
});
	
this.basicTriggers.push({
    cond: ( // 7.65 Done 
		this.C21.SEM > this.C21.UB1 && this.C13.SEM < this.C13.MA
    ),
    id: 7.65
});

this.basicTriggers.push({
    cond: ( // 7.53 done 
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA
    ),
    id: 7.53
});

this.basicTriggers.push({
    cond: ( //  7.44 done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA
    ),
    id: 7.44
});

this.basicTriggers.push({
    cond: ( //  7.46 Done
		this.C21.SEM < this.C21.LB1 && this.C13.SEM > this.C13.LB1 && this.C13.SEM < this.C13.MA 
		&& this.C13.SEM > this.C21.SEM && this.C34.SEM > this.C34.LB1
    ),
    id: 7.46
});

this.basicTriggers.push({
    cond: ( //  7.77 Done
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM > this.C21.UB1
		&& this.C21.UBLB2_Dir === 1
    ),
    id: 7.77
});

this.basicTriggers.push({
    cond: ( // 7.76 Done 
		this.C21.SEM > this.C21.UB1 && this.C13.SEM > this.C13.MA
		&& this.C21.UBLB2_Dir === 1
    ),
    id: 7.76
});

this.basicTriggers.push({
    cond: ( // 7.23 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1
    ),
    id: 7.23
});
	
this.basicTriggers.push({
    cond: ( // 7.21 Done
		this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA 
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1
    ),
    id: 7.21
});

this.basicTriggers.push({
    cond: ( // 7.18 Done
		this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA 
		&& this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1
    ),
    id: 7.18
});

this.basicTriggers.push({
    cond: ( // 7.22 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1
    ),
    id: 7.22
});

this.basicTriggers.push({
    cond: ( // 7.11 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C1.LB1 && this.C2.SEM > this.C2.UB1
    ),
    id: 7.11
});

this.basicTriggers.push({
    cond: ( // 7.34 Done should take long up blocks
		this.C13.SEM > this.C13.MA && this.C13.SEM < this.C13.UB1  
		&& this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 
		&& cStudy.C13_SEMTrend !== -0.1 && this.C13.SEM > this.C21.SEM
    ),
    id: 7.34
});

this.basicTriggers.push({
    cond: ( // 7.04 done
		this.C1.LB2 > this.C4.UB1 
    ),
    id: 7.04
});

this.basicTriggers.push({
    cond: ( // 7.14 Done should work on flat a lot of them 
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C3.SEM > this.C8.SEM 
		&& cStudy.C8_SEMTrend !== -0.1 
    ),
    id: 7.14
});

this.basicTriggers.push({
    cond: ( // 7.17 done 
		this.C21.MA < this.C89.MA && this.C21.MA > this.C89.LB1 && this.C21.SEM < this.C21.MA
		&& this.C89.SEM < this.C89.MA 
		&& this.C5.SEM > this.C5.LB1 
    ),
    id: 7.17
});

this.basicTriggers.push({
    cond: ( // 7.12 Done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1  
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C5.SEM >= this.C8.SEM
		&& cStudy.C8_SEMTrend !== -0.1 && cStudy.C5_SEMTrend !== -0.1
    ),
    id: 7.12
});

this.basicTriggers.push({
    cond: ( // 7.25 done
		this.C2.MA > this.C8.UB1 && this.L2.MA < this.C2.LB1
    ),
    id: 7.25
});

			/****************************************still not finished ****************************************************/


this.basicTriggers.push({
    cond: ( // 
		this.L2.MA > this.C5.LB2 && this.L2.MA < this.C8.LB2
    ),
    id: 6.56
});


this.basicTriggers.push({
    cond: ( //  
		/*&& this.C5.SEM > this.C21.UB1*/ this.C5.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C5.SEM < this.C5.MA && this.C8.SEM > this.C8.MA && this.C13.SEM > this.C13.MA
    ),
    id: 6.65
});


this.basicTriggers.push({
    cond: ( //  
		this.C21.SEM > this.C21.UB1 && this.C13.SEM > this.C13.UB1
		&& this.C8.MA > this.C21.UB1 && this.C8.SEM > this.C21.UB1
		&& cStudy.C5_SEMTrend !== -0.1 && this.C5.SEM > this.C5.MA
    ),
    id: 6.55
});


		// if( this.basicTrigger_DL1 === 0 && cLow > this.L2.MA && this.Side === 0 //  
		// && this.C8.MA > this.C21.UB1 && this.C5.MA > this.C21.UB1 
		// && this.C8.SEM > this.C8.UB1 && this.C5.SEM > this.C5.UB1 
		// && this.C21.SEM > this.C21.UB1 && cStudy.C21_SEMTrend !== -0.1 
		// && this.C3.SEM < this.C3.MA && this.C13.SEM > this.C13.UB1 
		// && this.L2.MA > this.C3.MA) {   
		
			// this.basicTrigger_DL1 = 6.58;
			// this.firsttime = 1
		// }



this.basicTriggers.push({
    cond: ( // 
		cStudy.C8_SEMTrend !== -0.1 && this.C8.SEM > this.C13.SEM 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C8.UB1
		&& this.C8.SEM > this.C8.UB1
    ),
    id: 6.79
});

// All matching IDs in original rule order.
this.matchingIds = this.basicTriggers
    .filter(trigger => trigger.cond)
    .map(trigger => trigger.id);

// Optional: select the first matching ID.
// Remove this section to choose an ID elsewhere.
if (this.matchingIds.length > 0) {
    this.basicTrigger_DL1 = this.matchingIds[0];
    this.firsttime = 1;
}