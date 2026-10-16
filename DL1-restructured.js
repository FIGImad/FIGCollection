		this.checkSignal = this.basicTrigger_DL1 === 0 && cLow > this.L2.MA && this.Side === 0;
		if (!this.checkSignal) return;

		if ( // 6.66 done 
		this.C5.SEM > this.C21.UB1 && this.C5.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C5.SEM < this.C5.MA && this.C8.SEM > this.C8.MA && this.C13.SEM > this.C13.MA
		&& !( this.C8.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C8.SEM < this.C8.MA && this.C13.SEM > this.C13.MA) ) {   
		
			this.basicTrigger_DL1 = 6.66;
			this.firsttime = 1
		}
		
		else if ( // 7.16 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 
		&& this.C1.SEM > this.C1.MA && this.C1.UB1 < this.C5.MA 
		&& ( (this.C5.SEM > this.C8.SEM) || (this.L2.MA > this.C8.SEM) ) ) {   
		
			this.basicTrigger_DL1 = 7.16;
			this.firsttime = 1
		}

		else if ( // 7.29 Done
		this.C3.MA > this.C8.UB1 && this.L2.MA < this.C3.LB1) {   
		
			this.basicTrigger_DL1 = 7.29;
			this.firsttime = 1
		}

		else if ( // 7.26 done
		this.C2.SEM > this.C5.UB2 && this.C1.SEM > this.C5.UB2
		&& this.C1.SEM < this.C2.SEM
		&& this.C4.SEM > this.C4.UB1 && this.C5.SEM > this.C5.UB1 && this.C8.SEM > this.C8.UB1) {   
		
			this.basicTrigger_DL1 = 7.26;
			this.firsttime = 1
		}

		else if ( // 7.24 Done
		this.C8.SEM < this.C5.LB1 && this.C13.SEM < this.C8.LB1 && this.C21.SEM < this.C13.LB1
		&& this.C34.SEM < this.C21.LB1 
		&& this.C8.SEM < this.C13.SEM && this.C13.SEM < this.C21.SEM && this.C21.SEM < this.C34.SEM
		&& this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2
		&& this.L1.MA < this.C13.LB2 && this.L1.MA < this.C21.LB2
		&& this.L2.MA < this.C13.LB2 && this.L2.MA < this.C21.LB2) {   
		
			this.basicTrigger_DL1 = 7.24;
			this.firsttime = 1
		}

		else if ( // 7.48 done 
		this.C21.SEM < this.C21.LB1 && this.C13.SEM > this.C13.LB1 && this.C13.SEM < this.C13.MA 
		&& this.C13.SEM < this.C21.SEM) {   
		
			this.basicTrigger_DL1 = 7.48;
			this.firsttime = 1
		}
		
		else if ( // 7.45 Done 
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C21_SEMLB1LB2_Stage === -1) {   
		
			this.basicTrigger_DL1 = 7.45;
			this.firsttime = 1
		}

		else if ( // 7.42 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1 && this.C5.SEM < this.C5.LB1 
		&& this.C13.SEM < this.C8.LB1 && this.C8.SEM < this.C5.LB1
		&& this.L2.MA < this.C8.SEM && this.L2.MA < this.C13.SEM
		&& this.L2.MA > this.C5.LB2 ) {   
		
			this.basicTrigger_DL1 = 7.42;
			this.firsttime = 1
		}

		else if ( // 7.43 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1 && this.C5.SEM < this.C5.MA && this.C5.SEM < this.C21.SEM 
		&& this.C21.SEM < this.C5.MA) {   
		
			this.basicTrigger_DL1 = 7.43;
			this.firsttime = 1
		}

		else if ( // 7.47 Done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA
		&& this.L2.MA > this.C21.LB2) {   
		
			this.basicTrigger_DL1 = 7.47;
			this.firsttime = 1
		}

		else if ( // 4.92 done				
		(cStudy.C2_C8_L_Cont_UB1 === 1 && cStudy.C3_C8_L_Cont_UB1 === 1) 
		&& this.C3.SEM > this.C5.SEM && (this.L2.MA > this.C1.MA || this.C1.SEM > this.C1.MA) ) {   
		
			this.basicTrigger_DL1 = 4.92;
			this.firsttime = 1
		}

		else if ( //  7.52 Done
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA < this.C89.MA ) {   
		
			this.basicTrigger_DL1 = 7.52;
			this.firsttime = 1
		}

		else if ( //  7.51 Done
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA
		&& this.C8.SEM < this.C8.LB1 && this.C1.MA > this.C21.LB2
		&& this.C8.SEM < this.C13.LB2 && this.C5.SEM < this.C13.LB2) {   
		
			this.basicTrigger_DL1 = 7.51;
			this.firsttime = 1
		}

		else if ( // 7.49 Done 
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA
		&& this.C8.SEM < this.C8.LB1 && this.C1.MA > this.C21.LB2
		&& this.C5.SEM > this.C8.SEM) {   
		
			this.basicTrigger_DL1 = 7.49;
			this.firsttime = 1
		}

		else if ( // 7.55 Done
		this.C2.MA < this.C13.LB1 && this.C8.SEM < this.C8.MA && this.C8.SEM > this.C8.LB1
		&& this.C13.SEM < this.C13.MA && this.C5.SEM < this.C5.LB1
		&& this.C21.SEM < this.C21.UB1 && this.C21.SEM > this.C21.MA) {   
		
			this.basicTrigger_DL1 = 7.55;
			this.firsttime = 1
		}

		else if ( // 7.54 Done
		this.C2.MA < this.C13.LB1 && this.C8.SEM < this.C8.MA && this.C8.SEM > this.C8.LB1
		&& this.C13.SEM < this.C13.MA && this.C5.SEM < this.C5.LB1
		&& this.C21.SEM < this.C21.MA
		&& this.C21.UBLB2_Dir === 1 && this.C21.UBLB1_Dir === 1) {   
		
			this.basicTrigger_DL1 = 7.54;
			this.firsttime = 1
		}
	
		else if ( // 7.69 Done
		this.C3.SEM > this.C5.UB2 && this.C2.SEM > this.C5.UB2 && this.C1.SEM > this.C5.UB2
		&& this.C5.SEM > this.C5.UB1) {   
		
			this.basicTrigger_DL1 = 7.69;
			this.firsttime = 1
		}

		else if ( // 7.59 Done 
		this.C13.SEM < this.C13.MA
		&& this.C5.SEM < this.C5.UB1 && this.C8.SEM < this.C8.UB1 
		&& (this.L2.MA > this.C13.UB2 || this.L1.MA > this.C13.UB2 || this.L3.MA > this.C13.UB2) ) {   
		
			this.basicTrigger_DL1 = 7.59;
			this.firsttime = 1
		}

		else if ( //  7.62 Done
		this.L2.MA < this.C21.MA && this.C13.SEM < this.C13.MA && this.C21.SEM > this.C21.UB1
		&& this.C13.UBLB1_Dir === 1 && this.C13.UBLB2_Dir === -1) {   
		
			this.basicTrigger_DL1 = 7.62;
			this.firsttime = 1
		}

		else if ( // 7.58 done
		this.C13.SEM < this.C13.LB1 && this.C21.SEM > this.C21.UB1 ) {   
		
			this.basicTrigger_DL1 = 7.58;
			this.firsttime = 1
		}

		else if ( // 7.61 done
		this.L2.MA < this.C21.MA && this.C13.SEM < this.C13.MA 
		&& this.C13.UBLB1_Dir === 1 && this.C13.UBLB2_Dir === -1) {   
		
			this.basicTrigger_DL1 = 7.61;
			this.firsttime = 1
		}

		else if ( //  7.74 Done
		this.C21.SEM > this.C21.UB1  
		&& this.L2.MA < this.C8.LB2 && this.L2.MA < this.C13.LB2
		&& this.C1.MA > this.C13.LB2 && this.C1.MA > this.C8.LB2) {   
		
			this.basicTrigger_DL1 = 7.74;
			this.firsttime = 1
		}

		else if ( // 4.02 Done 
		this.C5.SEM < this.C5.MA && this.C5.LB2 > this.C8.MA && this.L2.MA < this.C5.SEM
		&& this.C5.MA > this.C8.MA) {   
		
			this.basicTrigger_DL1 = 4.02;
			this.firsttime = 1
		}

		else if ( //  4.14 done need revision 4.1419 should short but risky
		this.C8.SEM > this.C8.UB1 && this.C5.SEM < this.C5.LB1) {   
		
			this.basicTrigger_DL1 = 4.14;
			this.firsttime = 1
		}

		else if ( // 4.01 Done
		this.C8.SEM > this.C8.UB1 
		&& this.C1.SEM < this.C8.UB1 && this.C2.SEM < this.C8.UB1 && this.C3.SEM < this.C8.UB1 
		&& this.C4.SEM < this.C8.UB1 && this.C5.SEM < this.C8.UB1 && this.C13.SEM < this.C8.UB1 && this.C21.SEM < this.C8.UB1) {   
		
			this.basicTrigger_DL1 = 4.01;
			this.firsttime = 1
		}

		else if ( // 7.15 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 
		&& this.C1.SEM > this.C1.MA && this.C1.UB1 < this.C5.MA ) {   
		
			this.basicTrigger_DL1 = 7.15;
			this.firsttime = 1
		}
	
		else if ( // 4.05 Done 
		this.C5.SEM > this.C5.UB1 && this.C3.SEM < this.C3.LB1) {   
		
			this.basicTrigger_DL1 = 4.05;
			this.firsttime = 1
		}

		else if ( // 7.36 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C5.UB1 && this.C2.SEM < this.C5.UB1 && this.C3.SEM < this.C5.UB1 
		&& this.C4.SEM < this.C5.UB1 && this.C13.SEM < this.C5.UB1 && this.C21.SEM < this.C5.UB1
		&& this.C8.SEM < this.C5.UB1) {   
		
			this.basicTrigger_DL1 = 7.36;
			this.firsttime = 1
		}

		else if ( // 7.35 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C5.UB1 && this.C2.SEM < this.C5.UB1 && this.C3.SEM < this.C5.UB1 
		&& this.C4.SEM < this.C5.UB1 && this.C13.SEM < this.C5.UB1 && this.C21.SEM < this.C5.UB1
		&& this.C8.SEM > this.C5.UB1) {   
		
			this.basicTrigger_DL1 = 7.35;
			this.firsttime = 1
		}

		else if ( // 4.08 done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM < this.C8.LB1) {   
		
			this.basicTrigger_DL1 = 4.08;
			this.firsttime = 1
		}

		else if ( // 4.07 Done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.UB1
		&& this.C8.SEM < this.C13.SEM && this.C5.SEM < this.C5.MA) {   
		
			this.basicTrigger_DL1 = 4.07;
			this.firsttime = 1
		}

		else if (  // 4.34 Done
		this.C21.SEM < this.C21.UB1 && this.C21.SEM < this.C5.UB2 && this.C21.SEM > this.C5.UB1
		&& this.L2.MA < this.C21.SEM && this.C13.SEM < this.C21.SEM && this.C8.SEM < this.C21.SEM
		&& this.C5.SEM < this.C21.SEM && this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA
		&& this.C13.SEM < this.C13.MA && this.C21.SEM > this.C21.MA) {   
			
			this.basicTrigger_DL1 = 4.34;
			this.firsttime = 1
		}

		else if (  // 4.36 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.C21.SEM > this.C21.MA && this.C13.SEM > this.C13.LB1 
		&& this.C8.SEM < this.C5.LB1 && this.C8.SEM < this.C2.MA
		&& this.C5.SEM < this.C3.LB1
		&& this.L2.MA > this.C89.MA) {   
		
			this.basicTrigger_DL1 = 4.36;
			this.firsttime = 1
		}

		else if (  // 4.48 Done
		this.C5.MA > this.C89.MA && this.C13.MA > this.C21.MA && this.C21.SEM > this.C21.MA
		&& this.C3.MA < this.C13.LB1 && this.C13.SEM < this.C13.LB1 && this.C13.SEM < this.C3.MA) {   
		
			this.basicTrigger_DL1 = 4.48;
			this.firsttime = 1
		}

		else if ( // 4.43 Done
		this.C2.MA > this.C3.MA && this.C3.MA > this.C5.MA && this.C5.MA < this.C13.MA
		&& this.C5.SEM > this.C5.MA && this.C8.SEM < this.C8.MA && this.C13.SEM < this.C13.LB1
		&& this.C5.SEM > this.C13.SEM && this.C8.SEM > this.C13.SEM
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.MA
		&& this.L2.MA > this.C2.SEM) {   
		
			this.basicTrigger_DL1 = 4.43;
			this.firsttime = 1
		}
	
		else if ( // 4.47 Done
		this.C1.SEM < this.C2.LB2 && this.C2.SEM < this.C3.LB2) {   
		
			this.basicTrigger_DL1 = 4.47;
			this.firsttime = 1
		}

		else if (  // 4.52 done  
		this.C5.SEM < this.C5.LB1 && this.C1.UB1 < this.C5.SEM && this.C1.UB1 < this.C3.SEM) {   
		
			this.basicTrigger_DL1 = 4.52;
			this.firsttime = 1
		}

		else if ( // 4.75 Done
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& this.C2.MA > this.C3.MA) {   
		
			this.basicTrigger_DL1 = 4.75;
			this.firsttime = 1
		}

		else if ( // 4.71 Done 
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& this.C1.UB1 < this.C4.LB1) {   
		
			this.basicTrigger_DL1 = 4.71;
			this.firsttime = 1
		}

		else if (  // 4.67 Done
		this.C2.MA < this.C13.LB2 && this.C2.MA < this.C5.LB1) {   
		
			this.basicTrigger_DL1 = 4.67;
			this.firsttime = 1
		}

		else if ( // 4.74 done
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 
		&& (this.C1.MA > this.C3.MA || this.C1.MA > this.C2.MA) ) {   
		
			this.basicTrigger_DL1 = 4.74;
			this.firsttime = 1
		}

		else if ( // 4.73 Done
		this.C2.MA < this.C21.LB2 && this.C3.MA < this.C21.LB2) {   
		
			this.basicTrigger_DL1 = 4.73;
			this.firsttime = 1
		}

		else if ( // 4.99 done  
		this.C1.SEM < this.C1.MA && this.C1.SEM < this.C5.LB2 
		&& (this.L2.MA < this.C3.SEM || this.L2.MA < this.C4.SEM) && this.L2.MA < this.C1.MA
		&& this.C3.SEM < this.C3.MA && this.C5.SEM < this.C5.MA
		&& this.C1.MA > this.C2.SEM && this.C1.MA > this.C3.SEM && this.C1.MA > this.C4.SEM && this.C1.MA > this.C5.SEM
		&& this.L2.MA > this.C2.SEM ) {   
		
			this.basicTrigger_DL1 = 4.99;
			this.firsttime = 1
		}

		else if ( // 4.87 done
		this.L1.MA < this.C5.LB2 && this.L2.MA < this.C4.LB2
		&& this.L3.MA < this.C2.LB2) {   
		
			this.basicTrigger_DL1 = 4.87;
			this.firsttime = 1
		}

		else if ( // 7.13 done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1  
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C5.SEM < this.C8.SEM
		&& cStudy.C8_SEMTrend !== -0.1 && cStudy.C5_SEMTrend !== -0.1) {   
		
			this.basicTrigger_DL1 = 7.13;
			this.firsttime = 1
		}

		else if ( // 4.85 Done 
		this.L1.MA > this.C5.LB2 && this.L2.MA > this.C4.LB2
		&& this.L3.MA < this.C2.LB2) {   
		
			this.basicTrigger_DL1 = 4.85;
			this.firsttime = 1
		}

		else if ( // 7.19 Done
		this.C5.SEM < this.C5.MA  
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.19;
			this.firsttime = 1
		}

		else if ( // 7.93 done
		this.L2.MA < this.C13.LB2 && this.C8.MA > this.C13.MA) { 
		
			this.basicTrigger_DL1 = 7.93;
			this.firsttime = 1
		}

		else if (  // 4.88 done
		this.C2.SEM < this.C4.LB2 && this.C1.SEM > this.C1.MA ) {   
		
			this.basicTrigger_DL1 = 4.88;
			this.firsttime = 1
		}

		else if (  // 4.86 done
		this.L2.MA < this.C3.LB2  ) {   
		
			this.basicTrigger_DL1 = 4.86;
			this.firsttime = 1
		}

		else if (  // 4.82 done
		this.C1.SEM < this.C2.LB2 && this.C2.SEM < this.C4.LB2 && this.C3.SEM > this.C5.LB2) {   
		
			this.basicTrigger_DL1 = 4.82;
			this.firsttime = 1
		}

		else if (  // 4.83 done
		this.C4.SEM < this.C2.LB1 && this.C2.SEM < this.C5.LB2 && this.C3.SEM > this.C5.LB2) {   
		
			this.basicTrigger_DL1 = 4.83;
			this.firsttime = 1
		}

		else if (  // 4.84 done
		this.C2.MA < this.C5.MA && this.C1.MA > this.C4.SEM && this.C1.UB1 < this.C5.SEM
		&& this.C2.SEM < this.C4.LB2 
		&& this.L2.MA > this.C8.LB2 && this.L2.MA > this.C5.LB2) {   
		
			this.basicTrigger_DL1 = 4.84;
			this.firsttime = 1
		}
	
		else if (  // 4.94 done
		this.C1.MA > this.C3.SEM && this.C1.MA > this.C2.SEM 
		&& this.C2.MA > this.C13.LB1 && this.C5.SEM > this.C8.LB2
		&& (pStudy.C2_C5_LLB2_Cont_MAC === 1 || cStudy.C2_C5_LLB2_Cont_MAC === 1) ) {   
		
			this.basicTrigger_DL1 = 4.94;
			this.firsttime = 1
		}

		else if (  // 4.91 done
		this.C2.MA > this.C13.LB1 
		&& (cStudy.C3_C5_LLB2_Cont_MAC === 1) ) {   
		
			this.basicTrigger_DL1 = 4.91;
			this.firsttime = 1
		}

		else if ( // 4.93 done
		this.C3.SEM <= Math.min(this.C13.LB2, this.C21.LB2) 
		&& this.L2.MA > this.C5.LB2  
		&& this.C1.MA > this.C4.SEM && this.C1.MA > this.C5.SEM
		&& this.C2.SEM < this.C5.LB2 ) {   
		
			this.basicTrigger_DL1 = 4.93;
			this.firsttime = 1
		}

		else if ( // 4.98 done 
		this.C3.SEM < this.C5.LB2 ) {   
		
			this.basicTrigger_DL1 = 4.98;
			this.firsttime = 1
		}

		else if ( // 7.84 done  
		this.C1.LB2 > this.C3.UB1 ) {   
		
			this.basicTrigger_DL1 = 7.84;
			this.firsttime = 1
		}

		else if ( // 4.06 done 
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.UB1
		&& this.C8.SEM < this.C13.SEM && this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA) {   
		
			this.basicTrigger_DL1 = 4.06;
			this.firsttime = 1
		}

		else if (  // 6.9 done
		this.L2.MAC_Pivot_Trend === -1 && this.C2.MAC_Pivot_Trend === -1 && this.C3.MAC_Pivot_Trend === -1 && this.C5.MAC_Pivot_Trend === 1 
		&& cStudy.C5_C3_MATrend === -1 && cStudy.C5_C2_MATrend === -1
		&& this.L2.MAC_SupRes > this.C5.MAC_SupRes && this.C2.MAC_SupRes > this.C5.MAC_SupRes
		&& (this.L2.MAC_SupRes > this.C2.MAC_SupRes || this.L2.MAC_SupRes > this.C3.MAC_SupRes) ) {
			this.firsttime = 1
			this.basicTrigger_DL1 = 6.9;
		}

		else if ( // 6.73 done
		this.C2.SEM > this.C5.UB2 && this.C3.SEM > this.C5.UB2
		&& this.C2.SEM > this.C1.UB2 && this.C3.SEM > this.C1.UB2
		/*&& this.C1.UB2 >= this.C5.UB2*/) {   
		
			this.basicTrigger_DL1 = 6.73;
			this.firsttime = 1
		}

		else if ( // 6.77 Done
		this.C1.SEM > this.C5.SEM && this.C1.SEM < this.C1.MA
		&& this.C5.SEM > this.C5.UB1) {   
		
			this.basicTrigger_DL1 = 6.77;
			this.firsttime = 1
		}
		
		else if ( // 6.76 Done
		this.C5.SEM < this.C8.SEM && this.C5.SEM < this.C13.SEM && this.C5.SEM < this.C21.SEM
		&& this.C5.SEM > this.C5.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM > this.C13.UB1 && this.C21.SEM > this.C21.UB1) {   
		
			this.basicTrigger_DL1 = 6.76;
			this.firsttime = 1
		}

		else if ( // 6.75 Done
		this.C1.MA > this.C8.UB2 ) {   
		
			this.basicTrigger_DL1 = 6.75;
			this.firsttime = 1
		}

		else if ( // 7.99 done
		cStudy.C5_Pivot_Status_Odd > 0) {   
		
			this.basicTrigger_DL1 = 7.99;
			this.firsttime = 1
		}
	
		else if (   // 1.99 done
		(cStudy.C1_C5_S_Cont_LB2 === -1 && cStudy.C1_C5_S_Cont_LB1 === -1) ) {   
		
			this.basicTrigger_DL1 = 1.99;
			this.firsttime = 1
		}

		else if (  // 1.98 done 
		cStudy.C1_C5_S_Cont_LB2 === -1 ) {   
		
			this.basicTrigger_DL1 = 1.98;
			this.firsttime = 1
		}

		else if (  // 1.97  done need more maybe conditions on short  
		cStudy.L1_C2_S_Cont_LB2 === -1 && cStudy.L1_C2_S_Cont_LB1 === -1) {   
		
			this.basicTrigger_DL1 = 1.97;
			this.firsttime = 1
		}

		else if (  // 1.97  done need more maybe conditions on short
		cStudy.L1_C2_S_Cont_LB2 === -1 && cStudy.L1_C3_S_Cont_LB2 === -1) {   
		
			this.basicTrigger_DL1 = 1.97;
			this.firsttime = 1
		}

		else if (  // 1.96 done 
		cStudy.L1_C3_S_Cont_LB1 === -1) {   
		
			this.basicTrigger_DL1 = 1.96;
			this.firsttime = 1
		}

		else if (  // 1.95 done 
		(cStudy.C2_C8_S_Cont_MAC === -1) ) {   
		
			this.basicTrigger_DL1 = 1.95;
			this.firsttime = 1
		}

		else if ( // 1.94 done  
		cStudy.C3_C8_S_Cont_LB1 === -1 && cStudy.C3_C5_S_Cont_LB1 === -1 
		&& this.C1.MA < this.C2.MA ) {   
		
			this.basicTrigger_DL1 = 1.94;
			this.firsttime = 1
		}

		else if ( // 7.05 done
		this.C5.SEM > this.C8.UB2 && this.C5.SEM > this.C13.UB2 && this.C5.SEM > this.C21.UB2 ) {   
		
			this.basicTrigger_DL1 = 7.05;
			this.firsttime = 1
		}

		else if ( // 7.01 Done
		this.C5.SEM > this.C8.UB2 && this.C1.SEM > this.C8.UB2 && this.C1.SEM > this.C1.MA 
		&& (this.C5.SEM < this.C13.UB2 || this.C5.SEM < this.C21.UB2) ) {   
		
			this.basicTrigger_DL1 = 7.01;
			this.firsttime = 1
		}

		else if ( // 6.72 done
		this.C1.SEM > this.C1.UB1 
		&& this.C5.SEM >= this.C5.MA && this.C5.SEM < this.C5.UB1
		&& this.C1.UB1 < this.C5.SEM && this.C1.UB2 > this.C5.SEM) {   
		
			this.basicTrigger_DL1 = 6.72;
			this.firsttime = 1
		}

		else if ( // 7.06 done
		cStudy.C5_SEMTrend !== -0.1 && this.C5.SEM > this.C8.SEM && this.C5.SEM > this.C13.SEM
		&& this.L2.MA > this.C5.SEM && this.L2.MA > this.C5.UB1
		&& this.C5.SEM > this.C5.UB1 /*&& this.C8.SEM < this.C8.UB1*/) {   
		
			this.basicTrigger_DL1 = 7.06;
			this.firsttime = 1
		}

		else if (  // 4.29 done
		this.C8.SEM > this.C8.UB1 
		&& this.C2.MA < this.C8.UB1 && this.C1.MA < this.C8.UB1
		&& this.C3.SEM > this.C8.UB1 && this.C4.SEM > this.C8.UB1 && this.C5.SEM > this.C8.UB1) {   
			
			this.basicTrigger_DL1 = 4.29;
			this.firsttime = 1
		}

		else if (  // 4.31 done
		this.C21.SEM < this.C21.LB1
		&& this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1) {   
			
			this.basicTrigger_DL1 = 4.31;
			this.firsttime = 1
		}

		else if (  // 7.86 done  //best trade is in 1.98  
		this.C2.LB1 > this.C5.UB1 ) {   
		
			this.basicTrigger_DL1 = 7.86;
			this.firsttime = 1
		}

		else if (   // 7.89 done
		this.C1.MA > this.C5.SEM && this.L2.MA > this.C5.SEM
		&& this.C1.SEM < this.C1.MA && this.L2.MA < this.C1.MA
		&& this.C3.MA > this.C5.MA && this.C2.MA > this.C3.MA && this.C1.MA > this.C2.MA 
		&& this.C5.SEM > this.C5.MA && this.C3.SEM > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.L2.MA > this.C5.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C2.MA ) {   
		
			this.basicTrigger_DL1 = 7.89;
			this.firsttime = 1
		}

		else if (  // 7.88 done 
		this.C1.MA > this.C5.SEM && this.L2.MA <= this.C5.SEM
		&& this.C1.SEM < this.C1.MA && this.L2.MA < this.C1.MA
		&& this.C3.MA > this.C5.MA && this.C2.MA > this.C3.MA && this.C1.MA > this.C2.MA 
		&& this.C5.SEM > this.C5.MA && this.C3.SEM > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.L2.MA > this.C5.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C2.MA ) {   
		
			this.basicTrigger_DL1 = 7.88;
			this.firsttime = 1
		}

		else if ( // 7.91 done 
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1 && cStudy.C5_SEMTrend !== -0.1 
		&& this.C1.SEM > this.C5.SEM && this.C1.SEM > this.C1.MA
		&& this.C4.SEM < this.C1.MA 
		&& this.L2.MA > this.C1.MA && this.L2.MA > this.C2.MA && this.L2.MA > this.C3.MA && this.L2.MA > this.C5.MA
		&& this.L2.MA > this.C5.SEM && this.L2.MA > this.C3.SEM
		&& this.C1.MA > this.C2.MA && this.C1.MA > this.C3.MA && this.C1.SEM > this.C3.SEM && this.C1.SEM > this.C4.SEM 
		&& this.C2.SEM > this.C3.SEM && this.C2.SEM > this.C5.SEM					
		&& this.C2.MA > this.C3.MA && this.C2.SEM > this.C2.MA
		&& this.C3.MA < this.C5.MA && this.C3.SEM > this.C5.SEM) { 
		
			this.basicTrigger_DL1 = 7.91;
			this.firsttime = 1
		}

		else if ( // 6.74 Done 
		this.C4.SEM > this.C5.SEM 
		&& this.C1.MA > this.C5.UB1 && this.C1.MA > this.C4.UB1
		&& this.C4.SEM < this.C4.UB1 && this.C5.SEM < this.C5.UB1
		&& cStudy.C4_SEMTrend === 1 && cStudy.C5_SEMTrend === 1) {   
		
			this.basicTrigger_DL1 = 6.74;
			this.firsttime = 1
		}

		else if ( // should go below 6.74 
		cStudy.C8_SEMTrend !== -0.1 && this.C8.SEM > this.C13.SEM 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C8.UB1
		&& this.C8.SEM > this.C8.UB1) {   
		
			this.basicTrigger_DL1 = 6.79;
			this.firsttime = 1
		}

		else if (   // 7.94 done need more specific and block adjustment
		this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA 
		&& this.L2.MA < this.C2.SEM 
		/*&& cStudy.C3_SEMShortTrend !== 1*/ && cStudy.C2_SEMShortTrend !== 1
		&& this.L3.MA < this.C1.SEM && this.L3.MA < this.C1.LB1
		&& this.C2.SEM < this.C2.MA  ) {  
			
			this.basicTrigger_DL1 = 7.94;
			this.firsttime = 1
		}

		else if (   // 7.97 done need more specific
		this.L2.MA > this.C5.MA && this.L2.MA > this.C1.MA 
		&& this.C1.MA > this.C5.MA && this.C1.SEM > this.C1.MA && this.C1.SEM > this.L2.MA && this.C1.SEM < this.C8.SEM
		&& this.C2.MA < this.C5.MA && this.C2.SEM > this.C2.MA && this.C2.SEM < this.C8.SEM
		&& this.C3.MA < this.C5.MA && this.C3.SEM > this.C3.MA && this.C3.SEM < this.C8.SEM
		&& this.C5.SEM < this.C5.MA && this.C5.SEM < this.C8.SEM) { 
		
			this.basicTrigger_DL1 = 7.97;
			this.firsttime = 1
		}

		else if ( //  6.64 Done
		this.C8.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C8.SEM < this.C8.MA && this.C13.SEM > this.C13.MA ) {   
		
			this.basicTrigger_DL1 = 6.64;
			this.firsttime = 1
		}
	
		else if (   // 7.95 done need more specific && adding the flat position
		this.C2.SEM > this.C2.MA
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM ) { 

			this.basicTrigger_DL1 = 7.95;
			this.firsttime = 1
		}

		else if (  // 7.92 done
		this.C2.SEM < this.C2.MA 
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM 
		&& this.L3.MA < this.C3.LB2 && this.C3.SEM > this.C5.SEM && this.C4.SEM > this.C5.SEM ) { 
		
			this.basicTrigger_DL1 = 7.92;
			this.firsttime = 1
		}
	
		else if ( // 7.96 // done allshort maybe a stop loss or select when to sell
		this.C2.SEM < this.C2.MA	
		&& this.C5.SEM < this.C5.MA && this.L2.MA < this.C1.MA && this.C1.SEM < this.C1.MA
		&& this.C3.SEM < this.C3.MA		
		&& this.C4.SEM < this.C4.MA
		&& this.L2.MA < this.C2.SEM 
		&& cStudy.C3_SEMShortTrend !== 1 && cStudy.C2_SEMShortTrend !== 1
		&& this.C2.MAC_Pivot_Trend === -1
		&& this.C2.SEM > this.C4.LB2 
		&& this.C2.SEM > this.C8.LB2) { 
		
			this.basicTrigger_DL1 = 7.96;
			this.firsttime = 1
		}
		
		else if ( 	// 4.76 done
		this.L2.MA < this.C5.LB2 && this.C3.MA > this.C13.MA
		&& this.L2.MA >= this.C13.MA
		&& (this.L3.MA > this.C1.LB1 || this.L3.MA > this.C3.LB2)
		&& this.C3.SEM < this.C3.MA && this.L2.MA > this.C8.LB2) {   
		
			this.basicTrigger_DL1 = 4.76;
			this.firsttime = 1
		}

		else if ( 	// 4.78 done
		this.L1.MA < this.C8.LB2 && this.L1.MA < this.C5.LB2) {   
		
			this.basicTrigger_DL1 = 4.78;
			this.firsttime = 1
		}

		else if ( 	// 4.77 done 	
		this.L2.MA < this.C8.LB2 && this.L2.MA < this.C5.LB2) {   
		
			this.basicTrigger_DL1 = 4.77;
			this.firsttime = 1
		}

		else if ( // 7.98 Done
		this.L2.MA < this.C5.LB2 && this.C3.MA > this.C13.MA) {  
		
			this.basicTrigger_DL1 = 7.98;
			this.firsttime = 1
		}

		else if ( // 4.68 Done revised 2
		this.C5.SEM < this.C1.MA && this.C1.SEM < this.C1.UB1
		&& this.C2.MA < this.C5.LB1 && this.C8.SEM < this.C2.MA
		&& this.C2.SEM > this.C5.SEM && this.C2.SEM > this.C3.SEM
		&& this.C2.SEM < this.C2.MA	) {   
		
			this.basicTrigger_DL1 = 4.68;
			this.firsttime = 1
		}

		else if ( // 4.96 done need to check short 42 41 39 
		this.C5.SEM < this.C13.LB2 && this.C5.SEM < this.C21.LB2 /*&& this.C5.SEM > this.C8.LB2*/) {   
		
			this.basicTrigger_DL1 = 4.96;
			this.firsttime = 1
		}

		else if ( // 7.28 Done before 7.63 
		this.C8.SEM < this.C5.LB1 && this.C13.SEM < this.C8.LB1 && this.C21.SEM < this.C13.LB1
		&& this.C34.SEM < this.C21.LB1 
		&& this.C8.SEM < this.C13.SEM && this.C13.SEM < this.C21.SEM && this.C21.SEM < this.C34.SEM) {   
		
			this.basicTrigger_DL1 = 7.28;
			this.firsttime = 1
		}
	
		else if ( //  7.63 Done
		this.C5.UB1 < this.C21.LB1 && this.C21.SEM < this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.63;
			this.firsttime = 1
		}

		else if ( // 4.69 Done
		this.C8.SEM > this.C13.SEM 
		&& (this.C8.SEM > this.C8.LB1 || this.C13.SEM > this.C8.LB1)
		&& this.C13.SEM < this.C13.LB1 && this.C13.SEM < this.C21.SEM
		&& this.C21.SEM < this.C21.MA
		&& this.C5.SEM < this.C5.LB1 && this.C5.SEM < this.C8.LB1) {   
		
			this.basicTrigger_DL1 = 4.69;
			this.firsttime = 1
		}

		else if ( // 4.72 Done need second revision
		this.C5.MA < this.C89.LB1 && this.C13.SEM > this.C5.MA ) {   
		
			this.basicTrigger_DL1 = 4.72;
			this.firsttime = 1
		}

		else if (  // 4.65 done need second revesion and short specs
		this.C13.SEM < this.C3.MA 
		&& this.C8.SEM < this.C2.MA && this.C5.SEM < this.C1.MA && this.C5.SEM < this.C3.LB1) {   
		
			this.basicTrigger_DL1 = 4.65;
			this.firsttime = 1
		}

		else if (  // 4.57 done test diferent location and cycles 
		this.C5.SEM < this.C5.LB1 && this.C1.UB1 < this.C5.SEM ) {   
		
			this.basicTrigger_DL1 = 4.57;
			this.firsttime = 1
		}

		else if (  // 4.56 done different locaion 
		this.C3.SEM < this.C3.MA && this.C1.UB1 < this.C3.SEM ) {   
		
			this.basicTrigger_DL1 = 4.56;
			this.firsttime = 1
		}

		else if (  // 4.49 Done
		this.C2.MA > this.C3.MA && this.C3.MA > this.C5.MA && this.C1.MA > this.C5.MA
		&& this.C1.UB1 < this.C2.MA && this.C1.SEM > this.C1.MA) {   
		
			this.basicTrigger_DL1 = 4.49;
			this.firsttime = 1
		}

		else if (  // 4.59 done 
		this.C5.MA > this.C89.MA && this.C13.MA > this.C89.MA && this.C21.MA > this.C89.MA
		&& this.C5.MA < this.C13.MA && this.C13.MA < this.C21.MA
		&& this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1) {    
		
			this.basicTrigger_DL1 = 4.59;
			this.firsttime = 1
		}
	
		else if ( // 4.42 done 
		this.C5.SEM < this.C89.LB2 && this.C13.SEM < this.C89.LB2
		&& this.C5.SEM < this.C13.SEM && this.C5.SEM < this.C21.SEM && this.C8.SEM < this.C13.SEM) {   
		
			this.basicTrigger_DL1 = 4.42;
			this.firsttime = 1
		}
	
		else if (  // 4.41 Done
		this.C2.MA < this.C3.MA && this.C3.MA < this.C5.MA && this.C1.MA < this.C5.MA
		&& this.C5.SEM < this.C5.LB1 && this.C8.SEM < this.C8.LB1 && this.C13.SEM < this.C13.LB1
		&& this.C1.UB1 < this.C2.MA /*&& this.C1.SEM > this.C1.MA*/) {   
			
			this.basicTrigger_DL1 = 4.41;
			this.firsttime = 1
		}

		else if (  // 4.39 done need revision after seeing all SEMC13 < C13LB1 SEMC21 < C21LB1 
		this.L2.MA > this.C13.MA && this.C13.SEM < this.C13.LB1) {   
			
			this.basicTrigger_DL1 = 4.39;
			this.firsttime = 1
		}

		else if (  // 4.54 done
		this.L2.MA > this.C3.MA && this.C2.SEM > this.C2.MA && this.C3.SEM > this.C3.MA 
		&& (this.C2.SEM > this.C3.SEM || this.C3.SEM > this.C4.SEM)
		&& this.C2.SEM > this.C5.SEM 
		&& this.C5.SEM > this.C8.SEM && this.C5.SEM > this.C13.SEM && this.C5.SEM > this.C21.SEM && this.C8.SEM > this.C13.SEM
		&& (this.L2.MA > this.C2.MA || this.L2.MA > this.C5.SEM) 
		&& this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1) {    
		
			this.basicTrigger_DL1 = 4.54;
			this.firsttime = 1
		}

		else if (  // 4.37 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.L2.MA > this.C89.MA
		&& this.C13.SEM < this.C13.LB1 && this.C21.SEM > this.C21.MA) {  
		
			this.basicTrigger_DL1 = 4.37;
			this.firsttime = 1
		}

		else if (  // 7.32 done
		this.C21.MA < this.C89.MA && this.C21.MA > this.C89.LB1 && this.C21.SEM < this.C21.MA
		&& this.C13.MA < this.C89.MA /*&& this.C13.MA > this.C89.LB1*/ && this.C13.SEM > this.C13.MA
		&& this.C89.SEM < this.C89.MA 
		&& this.C21.SEM < this.C89.SEM && this.C13.SEM > this.C21.SEM ) {   
		
			this.basicTrigger_DL1 = 7.32;
			this.firsttime = 1
		}

		else if (  // 4.53 Done
		this.C8.SEM < this.C8.LB1 && this.C5.SEM < this.C5.LB1 && this.C3.SEM < this.C3.LB1 
		&& this.C2.SEM < this.C2.LB1 && this.C1.SEM < this.C1.LB1
		&& this.L2.MA > this.C89.MA
		&& this.C13.SEM < this.C13.MA && this.C8.SEM < this.C5.LB1 && this.C21.SEM > this.C21.MA) {   
		
			this.basicTrigger_DL1 = 4.53;
			this.firsttime = 1
		}

		else if (  // 4.32 done
		this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM < this.C21.LB1) {   
			
			this.basicTrigger_DL1 = 4.32;
			this.firsttime = 1
		}

		else if (  // 4.28 done 
		this.L2.MA > this.C21.MA && this.C21.SEM < this.C21.LB1
		&& this.C8.SEM < this.C8.UB1) {   
			
			this.basicTrigger_DL1 = 4.28;
			this.firsttime = 1
		}

		else if (  // 4.27 Done
		this.C8.SEM > this.C8.UB1 && this.L2.MA > this.C8.UB1
		&& cStudy.C5_SEMTrend === 1.1 && cStudy.C4_SEMTrend === 1.1 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C5.SEM && this.L2.MA > this.C4.SEM
		&& this.C2.SEM > this.C3.SEM) {   
			
			this.basicTrigger_DL1 = 4.27;
			this.firsttime = 1
		}

		else if (  // 4.25 done
		this.C8.SEM > this.C8.UB1 /*&& this.L2.MA > this.C8.UB1*/
		&& cStudy.C3_SEMTrend !== -0.1 && this.C3.SEM < this.C3.UB1 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C3.SEM && this.L2.MA > this.C5.SEM) {   
			
			this.basicTrigger_DL1 = 4.25;
			this.firsttime = 1
		}
	
		else if (  // 4.19 done
		this.C8.SEM > this.C8.UB1 && this.L2.MA > this.C8.UB1
		&& cStudy.C3_C13_Flag_Trend_Lower === 1 && cStudy.C5_C21_Flag_Trend_Lower === -1
		&& this.C5.MA > this.C89.MA ) {   
			
			this.basicTrigger_DL1 = 4.19;
			this.firsttime = 1
		}

		else if (  // 4.33 Done
		cStudy.C3_C13_Flag_Trend_Lower === 1 && cStudy.C5_C21_Flag_Trend_Lower === -1 
		&& this.C3.MA < this.C13.MA && this.C13.SEM < this.C13.UB1 ) {   
			
			this.basicTrigger_DL1 = 4.33;
			this.firsttime = 1
		}

		else if (  // 4.35 Done
		this.C21.SEM < this.C21.LB1 && this.C13.SEM < this.C13.LB1
		&& this.L2.MA > this.C89.MA) {   
			
			this.basicTrigger_DL1 = 4.35;
			this.firsttime = 1
		}

		else if ( // 7.57 Done
		this.C21.SEM > this.C21.UB1  
		&& this.L2.MA < this.C8.LB2 && this.L2.MA < this.C13.LB2) {   
		
			this.basicTrigger_DL1 = 7.57;
			this.firsttime = 1
		}
	
		else if ( // 7.65 Done 
		this.C21.SEM > this.C21.UB1 && this.C13.SEM < this.C13.MA) {   
		
			this.basicTrigger_DL1 = 7.65;
			this.firsttime = 1
		}

		else if ( // 7.53 done 
		this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 && this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA && this.C13.MA > this.C89.MA) {   
		
			this.basicTrigger_DL1 = 7.53;
			this.firsttime = 1
		}

		else if ( //  7.44 done
		this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1 
		&& this.C13.SEM < this.C13.LB1
		&& this.C21.MA > this.C89.MA) {   
		
			this.basicTrigger_DL1 = 7.44;
			this.firsttime = 1
		}

		else if ( //  7.46 Done
		this.C21.SEM < this.C21.LB1 && this.C13.SEM > this.C13.LB1 && this.C13.SEM < this.C13.MA 
		&& this.C13.SEM > this.C21.SEM && this.C34.SEM > this.C34.LB1) {   
		
			this.basicTrigger_DL1 = 7.46; 
			this.firsttime = 1
		}

		else if ( //  7.77 Done
		this.C13.SEM > this.C13.UB1 && this.C8.SEM > this.C8.UB1 
		&& this.C21.SEM > this.C21.UB1
		&& this.C21.UBLB2_Dir === 1) {   
		
			this.basicTrigger_DL1 = 7.77;
			this.firsttime = 1
		}

		else if ( // 7.76 Done 
		this.C21.SEM > this.C21.UB1 && this.C13.SEM > this.C13.MA
		&& this.C21.UBLB2_Dir === 1) {   
		
			this.basicTrigger_DL1 = 7.76;
			this.firsttime = 1
		}

		else if ( // 7.23 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.23;
			this.firsttime = 1
		}
	
		else if ( // 7.21 Done
		this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA 
		&& this.C8.SEM < this.C8.UB1 && this.C8.SEM > this.C8.MA 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.21;
			this.firsttime = 1
		}

		else if ( // 7.18 Done
		this.C5.SEM < this.C5.UB1 && this.C5.SEM > this.C5.MA 
		&& this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.18;
			this.firsttime = 1
		}

		else if ( // 7.22 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM > this.C8.UB1 
		&& this.C13.SEM < this.C13.UB1 && this.C13.SEM > this.C13.MA 
		&& this.C21.SEM < this.C21.MA && this.C21.SEM > this.C21.LB1) {   
		
			this.basicTrigger_DL1 = 7.22;
			this.firsttime = 1
		}

		else if ( // 7.11 Done
		this.C5.SEM > this.C5.UB1 
		&& this.C1.SEM < this.C1.LB1 && this.C2.SEM > this.C2.UB1) {   
		
			this.basicTrigger_DL1 = 7.11;
			this.firsttime = 1
		}

		else if ( // 7.34 Done should take long up blocks
		this.C13.SEM > this.C13.MA && this.C13.SEM < this.C13.UB1  
		&& this.C21.SEM > this.C21.MA && this.C21.SEM < this.C21.UB1 
		&& cStudy.C13_SEMTrend !== -0.1 && this.C13.SEM > this.C21.SEM) {   
		
			this.basicTrigger_DL1 = 7.34;
			this.firsttime = 1
		}

		else if ( // 7.04 done
		this.C1.LB2 > this.C4.UB1 ) {   
		
			this.basicTrigger_DL1 = 7.04;
			this.firsttime = 1
		}

		else if ( // 7.14 Done should work on flat a lot of them 
		this.C5.SEM > this.C5.UB1 
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C3.SEM > this.C8.SEM 
		&& cStudy.C8_SEMTrend !== -0.1 ) {   // before 7.17
		
			this.basicTrigger_DL1 = 7.14; 
			this.firsttime = 1
		}

		else if ( // 7.17 done 
		this.C21.MA < this.C89.MA && this.C21.MA > this.C89.LB1 && this.C21.SEM < this.C21.MA
		&& this.C89.SEM < this.C89.MA 
		&& this.C5.SEM > this.C5.LB1 ) {    
		
			this.basicTrigger_DL1 = 7.17;
			this.firsttime = 1
		}

		else if ( // 7.12 Done
		this.C5.SEM > this.C5.MA && this.C5.SEM < this.C5.UB1  
		&& this.C8.SEM > this.C8.MA && this.C8.SEM < this.C8.UB1 
		&& this.C5.SEM >= this.C8.SEM
		&& cStudy.C8_SEMTrend !== -0.1 && cStudy.C5_SEMTrend !== -0.1) {   
		
			this.basicTrigger_DL1 = 7.12;
			this.firsttime = 1
		}

		else if ( // 7.25 done
		this.C2.MA > this.C8.UB1 && this.L2.MA < this.C2.LB1) {   
		
			this.basicTrigger_DL1 = 7.25;
			this.firsttime = 1
		}

			/****************************************still not finished ****************************************************/


		else if ( // 
		this.L2.MA > this.C5.LB2 && this.L2.MA < this.C8.LB2) {   
		
			this.basicTrigger_DL1 = 6.56;  
			this.firsttime = 1
		}


		else if ( //  
		/*&& this.C5.SEM > this.C21.UB1*/ this.C5.MA > this.C21.UB1 && this.C21.SEM > this.C21.UB1 
		&& this.C5.SEM < this.C5.MA && this.C8.SEM > this.C8.MA && this.C13.SEM > this.C13.MA) {   
		
			this.basicTrigger_DL1 = 6.65;
			this.firsttime = 1
		}


		else if ( //  
		this.C21.SEM > this.C21.UB1 && this.C13.SEM > this.C13.UB1
		&& this.C8.MA > this.C21.UB1 && this.C8.SEM > this.C21.UB1
		&& cStudy.C5_SEMTrend !== -0.1 && this.C5.SEM > this.C5.MA) {   
		
			this.basicTrigger_DL1 = 6.55;
			this.firsttime = 1
		}


		// if( this.basicTrigger_DL1 === 0 && cLow > this.L2.MA && this.Side === 0 //  
		// && this.C8.MA > this.C21.UB1 && this.C5.MA > this.C21.UB1 
		// && this.C8.SEM > this.C8.UB1 && this.C5.SEM > this.C5.UB1 
		// && this.C21.SEM > this.C21.UB1 && cStudy.C21_SEMTrend !== -0.1 
		// && this.C3.SEM < this.C3.MA && this.C13.SEM > this.C13.UB1 
		// && this.L2.MA > this.C3.MA) {   
		
			// this.basicTrigger_DL1 = 6.58;
			// this.firsttime = 1
		// }



		else if ( // 
		cStudy.C8_SEMTrend !== -0.1 && this.C8.SEM > this.C13.SEM 
		&& this.L2.MA > this.C8.SEM && this.L2.MA > this.C8.UB1
		&& this.C8.SEM > this.C8.UB1) {   
		
			this.basicTrigger_DL1 = 6.79;
			this.firsttime = 1
		}
