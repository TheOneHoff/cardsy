﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cardsy.Data.Games.Concentration
{
    [Table("Concentration")]
    public class ConcentrationGame
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int Size { get; set; } = 1;

        [Required]
        public int[] Solution { get; set; } = [];
    }

    public enum BoardSize
    {
        _2x2,
        _6x5,
        _7x4,
        _7x6
    }
}
