Đây là kế hoạch tiếp theo cho project của tôi.
-Hãy lập ra những thứ cần ưu tiên làm nhất sau đó đến mấy thứ khác. Trình bày hướng sẽ làm. 
-Nếu có gì chưa hiểu thì hãy hỏi để tôi xác nhận ngay rồi mới bắt đầu làm.

1. Xây dựng hiệu ứng kĩ năng ở màn hình combat. 
Khi tấn công thường, sẽ hiển thị 1 đường chém ngang vào kẻ thù được chọn. 
Khi dùng skill buff bản thân hoặc bôi vũ khí sẽ hiển thị vùng đỏ bùng lên nhẹ tại nhân vật. 
Khi dùng skill chém 4 nhát hoặc chém 2 nhát hay chém mạnh đều sẽ có hiệu ứng chém của kĩ năng, chém 4 nhát ngẫu nhiên thì bên kẻ thù sẽ vừa mất hp vừa có đường chém lên kẻ bị chọn ngẫu nhiên, chém 2 nhát vào mục tiêu thì sẽ hiển thị 2 đường chém lần lượt gây sát thương cho kẻ được chọn. Còn chém mạnh thì thay vì hiển thị chém thường thì sẽ dùng kiểu chém to hơn. 
Nếu kẻ thù tấn công thường thì sẽ hiển thị vết cào tại nhân vật và khi dùng kĩ năng sẽ hiển thị vết đập mạnh vào nhân vật. Nếu chém chí mạng hoặc bị dính skill kẻ thù thì sẽ khiến cammera rung nhẹ biểu thị mình vừa ăn một đòn đau.
.Hướng dẫn cách cài đặt trong unity.

2. Chỉnh sửa scense dungeon
-Sửa lỗi vô cửa khóa. Hiện tại scenes dungeon đang có cái cửa khóa, code đang thiết lập nó như và bức tường không thể tương tác được. Tôi muôn có thể đạp thẳng vào cửa khóa đó mặc dù nếu không có chia khóa thì sẽ không vào được và hiện thông báo cần chia. Cái tôi cần là xóa sương mù khi đạp phải cửa khóa để hiển thị cái cửa khóa thay vì làm nó như một bức tường tàn hình. 
-Tiếp theo là chìa khóa, tôi muốn tạo thêm phần image hiển thị. Nếu người chơi đạp phải chìa thì phần image đó sẽ tự động kích hoạt và hiển thị chìa khóa để nhận biết người chơi đã nhặt chìa rồi.
-Tiếp theo là phần các phần teshmesh hiển thị chỉ số nhân vật. Các hud hiển thị vàng, kinh nghiệm, nhu yếu phẩm, exp, điểm chỉ số. Thiết lập cho nó tắt hết các text đang xuất hiện tại hieracchy để khi bật game lên thì 2 dòng chữ không bị ghi đè lên nhau.
-Thêm phần hiểm thị nhiệm vụ được nhận tại town (tối đa 3) Là một 3 ô bên phải xếp dọc hiển thị tóm gọn nhiệm vụ như săn + icon quái + số lượng ví dụ 3/3 và tìm + icon vật phẩm + số lượng ví dụ 0/5, khi vào town nhiệm vụ được nhận sẽ hiện ra theo hàng dọc.
.Hướng dẫn cách cài đặt trong unity.

3. Chỉnh GuildUI
-Cập nhật phần giết kẻ thù. Chỉnh thành dạng đếm lên thay vì đếm xuống, ví dụ hiện tại nếu nhiệm vụ săn 5 quái khi nhận quái nó sẽ là 5/5, tôi muốn chỉnh lại thành 0/5 và khi giết 1 thì sẽ là 1/5.   