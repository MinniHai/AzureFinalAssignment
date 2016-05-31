using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public virtual ICollection<Document> Documents { get; set; }
    }

    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        public int CategoryId { get; set; }

        [StringLength(255)]
        [Column(TypeName = "nvarchar")]
        [Required]
        public string Title { get; set; }

        [Column(TypeName = "ntext")]
        public string Text { get; set; }

        [Column(TypeName = "ntext")]
        public string Html { get; set; }

        [StringLength(255)]
        [Column(TypeName = "nvarchar")]
        public string Link { get; set; }

        public virtual Category Category { get; set; }
        public virtual ICollection<Document_Keyword> Document_Keyword { get; set; }
    }

    public class Keyword
    {
        [Key]
        public int KeywordId { get; set; }

        [StringLength(100)]
        [Column(TypeName = "nvarchar")]
        public string Keyword_ { get; set; }
        public virtual ICollection<Document_Keyword> Document_Keyword { get; set; }

    }

    public class Document_Keyword
    {
        [Column(Order = 0), Key]
        [ForeignKey("Document")]
        public int DocumentId { get; set; }

        [Column(Order = 1), Key]
        [ForeignKey("Keyword")]
        public int KeywordId { get; set; }

        public double? TF { get; set; }

        public double? TFIDF { get; set; }

        public virtual Keyword Keyword { get; set; }
        public virtual Document Document { get; set; }
    }

    public class User_Preference
    {
        [Key]
        [StringLength(1000)]
        public string UserId { get; set; }

        [StringLength(255)]
        public string Preferences { get; set; }
    }
}
