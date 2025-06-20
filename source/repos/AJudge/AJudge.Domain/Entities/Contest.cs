﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AJudge.Domain.Entities
{
    public class Contest
    {
        
        public int ContestId { get; set; }
        public string Name { get; set; }
        public int GroupId { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }      
        public string Status { get; set; }
        public string Tutorial { get; set; }
        
        public int CreatorUserId { get; set; }
        // Navigation Properties

        [ForeignKey("CreatorUserId")]
        public User Creator { get; set; }

        [ForeignKey("GroupId")]        
        public Group Group { get; set; }
       public ICollection<Problem> Problems { get; set; } = new List<Problem>();
        public ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

        public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    }
}
