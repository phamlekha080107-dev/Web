using _25DH110384_WebStore.Models;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class UsersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Users (Danh sách tài khoản)
        public ActionResult Index()
        {
            var users = db.Users.ToList();
            return View(users);
        }

        // GET: Admin/Users/Details/admin (Chi tiết tài khoản)
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tìm tài khoản dựa trên Username (id)
            var user = db.Users.FirstOrDefault(u => u.Username == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
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