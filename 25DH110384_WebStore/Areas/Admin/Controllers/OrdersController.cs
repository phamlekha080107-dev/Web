using _25DH110384_WebStore.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace _25DH110384_WebStore.Areas.Admin.Controllers
{
    public class OrdersController : Controller
    {
        private QuanLiKhachHangEntities1 db = new QuanLiKhachHangEntities1();

        // GET: Admin/Orders
        // Hiển thị danh sách tất cả các đơn hàng (kèm thông tin khách hàng)
        public ActionResult Index()
        {
            var orders = db.Orders.Include(o => o.Customer).OrderByDescending(o => o.OrderDate);
            return View(orders.ToList());
        }

        // GET: Admin/Orders/Details/5
        // Xem chi tiết đơn hàng (kèm danh sách OrderDetail & Product)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy Order kèm theo thông tin Customer, OrderDetails và Product của từng item
            Order order = db.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.OrderDetails.Select(od => od.Product))
                            .FirstOrDefault(o => o.OrderID == id);

            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 1. Tìm đơn hàng cần xóa
            var order = db.Orders.Find(id);
            if (order != null)
            {
                // 2. Xóa tất cả chi tiết đơn hàng (OrderDetails) trước để tránh lỗi khóa ngoại (FK)
                var orderDetails = db.OrderDetails.Where(od => od.OrderID == id).ToList();
                if (orderDetails.Any())
                {
                    db.OrderDetails.RemoveRange(orderDetails);
                }

                // 3. Xóa đơn hàng
                db.Orders.Remove(order);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
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