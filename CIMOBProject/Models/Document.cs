﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CIMOBProject.Models
{
    public class Document
    {
        public int DocumentId { get; set; }

        public string Description { get; set; }

        [Required]
        public string FileUrl { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        public string StudentId { get; set; }

        public virtual Student Student { get; set; }
    }
}
