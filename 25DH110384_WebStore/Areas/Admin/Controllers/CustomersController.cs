using _25DH110384_WebStore.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class CustomersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Customers
        // Hiển thị danh sách tất cả khách hàng
        public ActionResult Index()
        {
            return View(db.Customers.ToList());
        }

        // GET: Admin/Customers/Details/5
        // Xem thông tin chi tiết của 1 khách hàng
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            _25DH110384_WebStore.Models.Customer customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }   
            return View(customer);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}