using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace _25DH110384_WebStore.Models
{
    public class ThongKeVM
    {
        public string LoaiHang { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaCaoNhat { get; set; }
        public decimal GiaThapNhat { get; set; }
        public decimal GiaTrungBinh { get; set; }
    }
}