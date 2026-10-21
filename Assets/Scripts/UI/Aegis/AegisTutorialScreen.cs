using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisTutorialScreen : AegisScreen
    {
        public AegisTabs tabs;
        public Text pageTitle, pageIndicator;
        public Text[] stepHeads, stepDescs;
        public Button prevButton, arrowNextButton, nextButton;

        int _tab, _page;
        bool _wired;

        struct PageData
        {
            public string title;
            public string[] steps;
            public PageData(string t, params string[] s) { title = t; steps = s; }
        }

        // Each step is "Heading|Description"
        static readonly PageData[][] Pages = new PageData[][]
        {
            new PageData[]
            {
                new PageData("Di chuyển quân cờ", "Chọn quân|Chạm vào quân cờ để chọn.", "Xem ô hợp lệ|Các ô di chuyển hợp lệ được highlight.", "Di chuyển|Chạm vào ô đích để di chuyển."),
                new PageData("Xoay và thu phóng", "Xoay bàn cờ|Kéo một ngón tay để đổi góc nhìn.", "Thu phóng|Chụm hai ngón để phóng to hoặc thu nhỏ.", "Đặt lại góc nhìn|Chạm đúp để về góc mặc định."),
                new PageData("Ăn quân", "Chọn quân tấn công|Chạm vào quân của bạn.", "Nhận diện mục tiêu|Quân đối phương có thể ăn sẽ sáng đỏ.", "Thực hiện|Chạm vào quân đối phương để ăn."),
                new PageData("Đồng hồ và lượt đi", "Theo dõi thời gian|Đồng hồ hai bên nằm phía trên bàn cờ.", "Đến lượt bạn|Quân của bạn phát sáng khi đến lượt.", "Hết giờ|Hết thời gian sẽ bị xử thua."),
                new PageData("Gợi ý và hoàn tác", "Gợi ý|Chạm biểu tượng bóng đèn để xem nước đi tốt.", "Hoàn tác|Quay lại nước vừa đi khi đấu với máy.", "Lịch sử nước đi|Xem lại toàn bộ ván ở màn hình kết quả."),
            },
            new PageData[]
            {
                new PageData("Chiếu và chiếu hết", "Chiếu|Vua đang bị quân đối phương tấn công.", "Thoát chiếu|Phải di chuyển, chắn hoặc ăn quân đang chiếu.", "Chiếu hết|Không còn cách thoát chiếu thì ván kết thúc."),
                new PageData("Nhập thành", "Điều kiện|Vua và Xe chưa từng di chuyển.", "Đường đi|Giữa Vua và Xe không có quân cản.", "Cách làm|Di Vua hai ô về phía Xe, Xe nhảy qua Vua."),
                new PageData("Hòa cờ", "Hết nước đi|Không bị chiếu nhưng không còn nước hợp lệ.", "Lặp thế cờ|Một thế cờ lặp lại ba lần.", "Thiếu lực lượng|Không bên nào đủ quân để chiếu hết."),
            },
            new PageData[]
            {
                new PageData("Quân Tốt", "Đi thẳng|Tiến một ô, nước đầu tiên được đi hai ô.", "Ăn quân|Ăn chéo phía trước một ô.", "Phong cấp|Đến cuối bàn cờ sẽ đổi thành quân khác."),
                new PageData("Quân Xe", "Đi thẳng|Đi ngang hoặc dọc không giới hạn số ô.", "Bị cản|Không thể nhảy qua quân khác.", "Nhập thành|Phối hợp cùng Vua để nhập thành."),
                new PageData("Quân Mã", "Hình chữ L|Đi hai ô theo một hướng rồi một ô vuông góc.", "Nhảy qua quân|Là quân duy nhất nhảy qua quân khác.", "Sức mạnh|Mạnh nhất khi đứng gần trung tâm."),
                new PageData("Quân Tượng", "Đi chéo|Đi chéo không giới hạn số ô.", "Giữ màu ô|Chỉ đi trên một màu ô duy nhất.", "Cặp Tượng|Hai Tượng phối hợp rất mạnh."),
                new PageData("Quân Hậu", "Linh hoạt|Đi ngang, dọc và chéo không giới hạn.", "Mạnh nhất|Quân có sức mạnh lớn nhất trên bàn cờ.", "Lưu ý|Đừng đưa Hậu ra quá sớm."),
                new PageData("Quân Vua", "Một ô|Đi một ô theo mọi hướng.", "Cần bảo vệ|Mất Vua là thua ván.", "Không tự vào chiếu|Không thể đi vào ô bị tấn công."),
            },
            new PageData[]
            {
                new PageData("Chuẩn bị", "Cấp quyền camera|Cho phép ứng dụng sử dụng camera.", "Tìm mặt phẳng|Di chuyển điện thoại chậm trên mặt bàn.", "Ánh sáng|Chọn nơi đủ sáng để theo dõi tốt hơn."),
                new PageData("Đặt bàn cờ", "Chạm để đặt|Chạm vào mặt phẳng đã nhận diện.", "Điều chỉnh|Kéo để di chuyển, chụm để đổi kích thước.", "Xác nhận|Chạm Đặt bàn cờ để bắt đầu ván."),
                new PageData("Chơi và chụp ảnh", "Đi quân|Chạm quân cờ rồi chạm ô đích.", "Chụp ảnh|Dùng nút máy ảnh để lưu khoảnh khắc.", "Quay video|Ghi lại ván đấu để chia sẻ."),
            },
        };

        public override void OnShown()
        {
            if (!_wired) { Wire(); _wired = true; }
            _tab = 0;
            _page = 0;
            tabs.Select(0, false);
            Render();
        }

        void Wire()
        {
            tabs.onChanged = i => { _tab = i; _page = 0; Render(); };
            prevButton.onClick.AddListener(Prev);
            arrowNextButton.onClick.AddListener(NextPage);
            nextButton.onClick.AddListener(Next);
        }

        void Render()
        {
            var list = Pages[_tab];
            var p = list[_page];
            pageTitle.text = p.title;
            for (int i = 0; i < stepHeads.Length; i++)
            {
                bool has = i < p.steps.Length;
                stepHeads[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;
                var parts = p.steps[i].Split('|');
                stepHeads[i].text = parts[0];
                stepDescs[i].text = parts.Length > 1 ? parts[1] : "";
            }
            pageIndicator.text = (_page + 1) + " / " + list.Length;
            SetArrow(prevButton, _page > 0);
            SetArrow(arrowNextButton, _page < list.Length - 1);
        }

        static void SetArrow(Button b, bool on)
        {
            b.interactable = on;
            var g = b.targetGraphic;
            if (g != null) { var c = g.color; c.a = on ? 1f : 0.35f; g.color = c; }
        }

        void Prev() { if (_page > 0) { _page--; Render(); } }

        void NextPage() { if (_page < Pages[_tab].Length - 1) { _page++; Render(); } }

        void Next()
        {
            if (_page < Pages[_tab].Length - 1) { _page++; Render(); }
            else if (_tab < Pages.Length - 1) { tabs.Select(_tab + 1); }
            else { var m = AegisUIManager.Instance; if (m != null) m.Back(); }
        }
    }
}
