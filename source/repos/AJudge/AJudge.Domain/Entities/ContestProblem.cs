﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AJudge.Domain.Entities
{
    
    public class ContestProblem
    {
        
         public int ContestProblemId { get; set; }
        public int ContestId { get; set; }
        public int ProblemId { get; set; }
        public  Contest Contest { get; set; }

       // [ForeignKey("ProblemId")]
        public  Problem Problem { get; set; }
    }
}
