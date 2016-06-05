using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class category
    {
        [Key]
        public int CategoryId { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        public virtual ICollection<document> Documents { get; set; }
        public virtual ICollection<ad> Ads { get; set; }
    }

    public class document
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

        public virtual category Category { get; set; }
        public virtual ICollection<doc_keyword> Document_Keyword { get; set; }
    }

    public class keyword
    {
        [Key]
        public int KeywordId { get; set; }

        [StringLength(100)]
        [Column(TypeName = "nvarchar")]
        public string Keyword { get; set; }
        public virtual ICollection<doc_keyword> Document_Keyword { get; set; }

    }

    public class doc_keyword
    {
        [Column(Order = 0), Key]
        [ForeignKey("Document")]
        public int DocumentId { get; set; }

        [Column(Order = 1), Key]
        [ForeignKey("Keyword")]
        public int KeywordId { get; set; }

        public double? TF { get; set; }

        public double? TFIDF { get; set; }

        public virtual keyword Keyword { get; set; }
        public virtual document Document { get; set; }
    }

    public class user_preference
    {
        [Key]
        [StringLength(1000)]
        public string UserId { get; set; }

        [StringLength(255)]
        public string Preferences { get; set; }
    }

    public class ad
    {
        [Key]
        public int AdId { get; set; }
        
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public int Price { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(2083)]
        public string ImageURL { get; set; }

        [StringLength(2083)]
        public string ThumbnailURL { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime PostedDate { get; set; }

        public int CategoryId { get; set; }

        [StringLength(12)]
        public string Phone { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string Keyword { get; set; }

        public virtual category Category { get; set; }
    }
}
