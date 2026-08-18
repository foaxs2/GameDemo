Đây là kế hoạch tiếp theo cho project của tôi.
-Hãy lập ra những thứ cần ưu tiên làm nhất sau đó đến mấy thứ khác. Trình bày hướng sẽ làm. 
-Nếu có gì chưa hiểu thì hãy hỏi để tôi xác nhận ngay rồi mới bắt đầu tạo bảng kế hoạch
-Vì tôi vẫn chỉ là người mới dùng Unity nên sẽ có nhiều định nghĩa mà tôi chưa biết và những thứ tôi liệt kê ở dưới có thể tôi chưa biết cách thiết lập tại unity thể nào nên hãy hướng dẫn cụ thể.

1. Xây dựng hiệu ứng kĩ năng ở màn hình combat. 
Khi tấn công thường, sẽ hiển thị 1 đường chém ngang vào kẻ thù được chọn. 
Khi dùng skill buff bản thân hoặc bôi vũ khí sẽ hiển thị vùng đỏ bùng lên nhẹ tại nhân vật rồi biến mất. 
Khi dùng skill chém 4 nhát hoặc chém 2 nhát hay chém mạnh đều sẽ có hiệu ứng chém của kĩ năng, chém 4 nhát ngẫu nhiên thì bên kẻ thù sẽ vừa mất hp vừa có đường chém (4 hoạt ảnh đường chém lần lượt) lên kẻ bị chọn ngẫu nhiên, chém 2 nhát vào mục tiêu thì sẽ hiển thị 2 đường chém lần lượt gây sát thương cho kẻ được chọn. Còn chém mạnh thì thay vì hiển thị chém thường thì sẽ dùng kiểu chém to hơn. 
Nếu kẻ thù tấn công thường thì sẽ hiển thị vết cào tại nhân vật rồi biến mất (vết cào tồn tại 1 giây ) và khi dùng kĩ năng sẽ hiển thị vết đập mạnh (tương tự với vết cào và màn hình sẽ run nhẹ biểu thị cho việc bị trúng đòn) vào nhân vật. Nếu chém chí mạng hoặc bị dính skill kẻ thù thì sẽ khiến cammera rung nhẹ biểu thị mình vừa ăn một đòn đau.
Câu hỏi: Mỗi hiệu ứng ở trên ta cũng đều cần vẽ 1 asset riêng cho từng thằng đúng không?

1.1. Cải thiện hiệu ứng trong scenes combat.
-Rung màn hình (Screen Shake): Khi người chơi bị dính sát thương lớn (Crit) hoặc bị trúng đòn nặng.
-Animation nhẹ cho UI: Các thanh máu (HP Bar) nên có hiệu ứng "giảm dần" (white bar) thay vì tụt ngay lập tức để người chơi thấy rõ lượng máu đã mất.
.Hướng dẫn cách cài đặt trong unity.

2. Chỉnh sửa scense dungeon và cải thiện scenes town.
-Sửa lỗi vô cửa khóa. Hiện tại scenes dungeon đang có cái cửa khóa, code đang thiết lập nó như và bức tường không thể tương tác được. Tôi muôn có thể đạp thẳng vào cửa khóa đó mặc dù nếu không có chia khóa thì sẽ không vào được và hiện thông báo cần chia. Cái tôi cần là xóa sương mù khi đạp phải cửa khóa để hiển thị cái cửa khóa thay vì làm nó như một bức tường tàn hình. Dễ hiểu hơn thì tôi muốn cái tile cửa khóa đó không phải là 1 vật cản mà là thứ có thể chạm vô được và hiện thông báo (cái này đã làm rồi) nếu từ chối vào cửa để ra hoặc không có chia khóa thì người chơi có thể tiếp tục đi tiếp.
-Tiếp theo là chìa khóa, tôi muốn tạo thêm phần image hiển thị. Nếu người chơi đạp phải chìa khóa thì phần image sẽ hiển thị icon chìa khóa góc dưới để nhận biết người chơi đã nhặt chìa rồi và khi sử dụng nó để mở cửa thì icon biến mất.
-Tiếp theo là phần các phần teshmesh hiển thị chỉ số nhân vật. Các hud hiển thị vàng, kinh nghiệm, nhu yếu phẩm, exp, điểm chỉ số. Thiết lập cho nó tắt hết các text đang xuất hiện tại hieracchy để khi bật game lên thì 2 dòng chữ không bị ghi đè lên nhau.
-Thêm phần hiểm thị nhiệm vụ được nhận tại town (tối đa 3) Là một 3 ô bên phải xếp dọc hiển thị tóm gọn nhiệm vụ như săn + icon quái + số lượng ví dụ 3/3 và tìm + icon vật phẩm + số lượng ví dụ 0/5, khi vào town nhiệm vụ được nhận sẽ hiện ra theo hàng dọc.
-Phía bên phải dưới sẽ là nơi chưa ô vật phẩm loại 2. *Vật phẩm loại 2 là vật phẩm sẽ không nằm trong mục item hoặc equip của người chơi hiển thị trong màn hình combat trừ Bình máu. Vật phẩm loại 2 sẽ là vật phẩm có thể bán được cho thương nhân như antifact (định nghĩa là antifact là những vật phẩm không sử dụng được ở dungeon và sẽ chiếm 1 ô cho mỗi vật phẩm tại dungeon. Tác dụng của nó là sau khi khám phá xong dungeon có thể dùng nó để trao đổi lấy vàng từ chủ shop. Tạo một game object mới cho phép thêm mới antifact như tên, icon, số vàng bán ra, một dòng mô tả và có thể thêm 1 số thứ bạn có thể gợi ý cho tôi nên thêm phần nào hoặc đã đủ rồi thì có thể mặc định 4 thứ trên) hoặc dùng được trong dungeon như food hoặc item kích hoạt (vì game chưa có nên tôi sẽ định là sẽ có 1 vật phẩm kích hoạt như Đèn soi sáng, nó có tác dụng đưa tầng event trong dungeon + 1 để hiển thị các event lên trên cùng với người chơi mà không làm mất lớp sương mù. Tạo mới 1 game object cho phần vật phẩm kích hoạt, bao gồm tên, icon, giá bán và mô tả ngắn gọn. Nó sẽ có thể sử dụng được trong dungeon và sau khi sử dụng nó để kích hoạt tính năng, icon sẽ biến mất khỏi túi đồ của người chơi và thêm 1 dòng thông báo trong log là bạn đã sử dụng Đèn soi sáng để mở rộng khu vực quan sát) Và các loại vật phẩm dùng trong town như thép dùng để nâng cấp trang bị hoặc đồ để chế tác vật phẩm, những thứ này sẽ không xuất hiện trong combat mà sẽ hiển thị trên dungeon chiếm 1 slot.

*Đánh quái vật bây giờ sẽ có khả năng rơi ra vật phẩm khác, các vật phẩm rơi ra được bỏ vào 1 pool để tôi gom các vật phẩm có thể rới từ quái vào và sẽ có tỉ lệ rơi ra 1 hoặc 2 hoặc 3 món đồ nếu may mắn. Tỉ lệ rơi ra 1 món đồ trong danh sách đó khi tiêu diệt 1 con quái sẽ là 10% (con số này có thể chỉnh sửa được và danh sách sẽ được bốc ngẫu nhiên vật phẩm rớt ra). Nếu vật phẩm loại 1 thì khi quái rơi sẽ không chiếm slot tại dungeon mà vào trong túi người chơi ,có thể kiểm tra bằng cách vào item và xem có vật phẩm mới nào được thêm vào không hoặc số lượng vật phẩm có tăng lên không. Nếu là vật phẩm loại 2 thì khi đánh xong quái vật thì sẽ thấy nó chiếm lấy 1 ô tại UI scenes dungeon. 

-Ngươi chơi sẽ mặc định có 6 slot có thể nâng cấp được tại khu vực rèn mỗi lần nâng cấp sẽ tốn 1 lượng vàng (do tôi cài đặt mặc định là 150) và được cộng 2 slots mỗi lần nâng cấp.
Và mỗi lần nâng cấp giá vàng cộng thêm 50 so với giá vàng được đặt trước đó.

2.1.
-scenes town giờ sẽ có thêm 2 chức năng mới là tiệm rèn vũ khí và Tiệm thuốc.
*Tiệm rèn vũ khí sẽ có khả năng rèn vũ khí hoặc giáp chỉ có 2 ô button mới được thêm vào và khi bấm vào button có ảnh giáp hoặc vũ khí thì sẽ nâng cấp chỉ số ở ô tương ứng mỗi cấp sẽ tốn 1, 2, 3 thép để rèn áp dụng cho cả giáp và vũ khí và số vàng để nâng cấp mặc định lần lượt là 100 180 320 có thể thay đổi được (tối đa cộng 3 cấp cho từng loại). Lưu ý phải có Thép mới rèn được và thép là vật phẩm loại 2. Khi nâng vào button vũ khí thì sẽ cộng chỉ số tầng công lên 1 rồi 2 rồi 5 theo cấp tương ứng (mặc định và con số giữa 3 mốc tôi có thể chỉnh sửa được) và giáp khi nâng lên thì sẽ cộng hp vào người chơi. Mốc Hp lần lượt cộng lên là 3, 6, 12 (có thể thay đổi). Đặc biệt nếu người chơi không dùng thép mà dùng Orichalcum (vật phẩm loại 2 khác) thì chỉ số khi dùng nó vào vũ khí vẫn sẽ tăng tấn công như bình thường là 1, 2, 5 nhưng nó sẽ cộng thêm vào 1 chỉ số khác là tỉ lệ bạo kích và mỗi lần dùng nó sẽ cộng thêm 2% tỉ lệ bạo kích vào mỗi cấp và khi dùng để rèn sẽ không tốn phí để nâng lên. Giáp hoạt động tương tự nhưng chỉ số cộng thêm sẽ là chỉ số Né tránh Evasion. Mỗi cấp nâng chỉ số này lên 2% và cũng không tốn phí để rèn.

*Tiệm thuốc có thể bán thuốc hồi máu (chuyển việc bán thuốc hồi máu từ guild sang tiệm thuốc) và có thể chế tạo vật phẩm. Ô chế tạo sẽ gồm món A và món B kết hợp với nhau và có công thức của nó. (Hiện tại chức năng này sẽ làm sau vì tôi chưa nghĩ ra được công thức chế tạo cụ thể nào cho các vật phẩm. Nhưng vẫn sẽ có UI gồm button cho 2 ô để khi bấm vào có thể chọn vật phẩm ghép và một nút xác nhận nữa để ghép. Tôi một button khi bấm vào sẽ hiển thị một panel nhỏ gồm các công thức A + B -> C Mặc dù chưa có ý tưởng nào cho sự kết hợp nên hãy gợi ý cho tôi một số công thức và vật phẩm có thể chế tạo. Các vật phẩm chế tạo ở đây chỉ có thể chế ra vật phẩm loại 2 còn loại 1 chỉ có thể mua hoặc tìm thấy trong dungeon nhưng cũng có thể dùng vật phẩm loại 1 để chế với vật phẩm khác)

.Hướng dẫn cách cài đặt và xây giao diện trong unity có thể gợi ý giao diện nó sẽ gồm gì và trông như thế nào trước.
.Cho tôi biết sẽ có thêm những game object nào mới và nó sẽ gồm những gì.

3. Chỉnh GuildUI
-Cập nhật phần giết kẻ thù. Chỉnh thành dạng đếm lên thay vì đếm xuống, ví dụ hiện tại nếu nhiệm vụ săn 5 quái khi nhận quái nó sẽ là 5/5, tôi muốn chỉnh lại thành 0/5 và khi giết 1 thì sẽ là 1/5. (đảo ngược logic hiện tại)   

4. Bổ sung scenes test chức năng.
-Nhân bản từ scenes combat tên là  testroom, chứa các menu khi bắt đầu có thể tự bốc các kẻ thù, trang bị, vật phẩn để test. Có một cái menu bên trong chỉnh để lấy được mấy thứ đó. Menu chọn trang bị, vật phẩm, kẻ thù xuất hiện sẽ có dạng hộp thả xuống, khi bấm sẽ thả xuống các thú mà tôi đã thêm vào và một button để kích hoạt thứ tôi đã chọn.
-Một cái để có thể tùy chỉnh chỉ số của nhân vật tùy thích và ngay lập tức áp dụng lên nhân vật khi bấm button xác nhận.
-Các quái ngẫu nhiên thì vẫn sinh ngẫu nhiên nếu như tôi không thiết lập ô chọn kẻ thù nào hết. Ô chọn kẻ thù cũng sẽ có 3 ô chọn tượng trưng cho 3 vị trí nó sẽ xuất hiện.
-ít nhất nó là ý tưởng sơ bộ của tôi về sceneTest, vì tôi còn non kinh nghiệm nên có gì cần chỉnh sửa lại thì hãy báo tôi.
-Hướng dẫn cách cài đặt và xây dựng UI trong unity.
-Câu hỏi có cần tạo thêm 1 scenes test thứ 2 nhân bản ra từ scenes dungeon để chia tách 2 phần của nó ra không?

5. Chỉnh lại logic skill kẻ thù.
-Xóa bỏ thời gian chờ ban đầu ví dụ nếu tôi set thời gian là 2 thì sau 2 turn quái mới được tấn công. (xóa cái đấy)
-Thay đổi tỉ lệ kích hoạt skill của kẻ thù bậy giờ sẽ luôn là 100%.
-Khi mới vô kẻ thù sẽ có countdown skill = Với turn chờ của skill đó. Ví dụ kĩ năng cắn của nhện có thời gian chờ là 3 turn thì mới vô game nhện không thể xài mà đợi đúng 3 turn mới được xài.
-Thêm mới UI hiển thị kĩ năng của kẻ thù. Dạng bong bóng suy nghĩ. Hiển thị khi quái vật chuẩn bị dùng skill trước 1 turn. Ví dụ nhện sẽ xài skill sau 3 turn thì khi game đếm được 2 turn thì hiển thị cái UI kĩ năng đó ra cho người chơi biết được để chuẩn bị biện pháp đối phó. UI hiển thị là icon Skill đặc trưng của kẻ thù đó. Chỉnh thêm enemieData cho phép gán icon skill cho nó.
-Thêm một skill mới cho một loại kẻ thù mới (Tê tê) tên skill là Bọc giáp, mô tả: khi sử dụng kĩ năng, tăng cho tê tê 50% giảm sát thương (không có tác dụng với các debuff dạng độc hoặc bỏng) và phản dmg cho người chơi nếu người chơi tấn công vào. Mức phản sát thương được tính bằng công thức: sát thương tê tê + sát thương nhận được/1.5 + với các debuff mà người chơi đang nhận, sẽ bị giảm sát thương nếu người chơi đang có chỉ số def, mức giảm tương ứng.

6. Cải thiện logic sanity,
-HIện tại khi sen thấp ta chỉ bị giới hạn việc nhìn thông tin kẻ thù chuyển sang ???
-Bây giờ tôi muốn khi sen thấp -> Icon của Enemie chuyển về mặc định hết (trừ boss). 
-Ẩn UI của thanh AP kẻ thù. 
-Ẩn UI hiển thị kĩ năng của kẻ thù (dạng bong bóng) làm người chơi không biết rằng kẻ thù sẽ chuẩn bị tung kĩ năng khi nào và dùng kĩ năng gì cho bản thân.
-Khi sen tăng lại mức an toàn thì hiển thị lại bình thường.

7. Thêm loại sự kiện mới.
-Sự kiện đánh đổi: Đánh đổi HP, tiền, vật phẩm, giảm/tăng chỉ số. Ví dụ: "Bạn có dám hiến tế 20 HP để đổi lấy 5 điểm ATK vĩnh viễn cho tầng này không?" Trả lời có hoặc không và tỉ lệ của cả 2 là 100%. Thêm lập trình cho kiểu loại sự kiện đánh đổi này.
-Sự kiện va chạm: một sự kiện xuất hiện sẽ có khả năng phải đánh một con quái vật đặc biệt không nằm trong pool random có thể set cho từng loại vào được. Ví dụ bỏ một con quái tinh anh vào sự kiện và khi người chơi dẫm phải và chọn kích hoạt sự kiện thì sẽ chuyển vào màn hình combat với con quái vật được thiết lập sẵn ở đó. (số lượng có thể thiết lập từ 1-3 con) Nếu người chơi chọn bỏ chạy thì sự kiện đấy sẽ không biến mất. Nhưng nếu dẫm phải một lần nữa thì chắc chắn sẽ vào combat với chính con quái đấy mà không cần thông qua tỉ lệ hay lựa chọn nào. Thiết lập cho kiểu sự kiện này sẽ xuất hiện ở tầng 5, 10, 15, 20 và tôi có thể tùy ý thay đổi số tầng mà sự kiện này có thể xuất hiện.

8. Thay đổi dungeon.
-Tăng số lượng tầng của dungeon từ 10 -> 20
-Loại bỏ boss mỗi 5 tầng như nhện nữ vương và rồng, chuyển 2 con boss đấy lần lượt tại tầng 10 và tầng 20.








Mục đang xem xét:
- Thêm trang bị, vật phẩm và sự kiện
- Thêm vài dòng mới cho mục enemy data giúp tự buff chỉ số, các ô mới với chức năng tự tăng khả năng buff của kẻ thù, có duration, mức tăng (ví dụ tăng thêm 3 dmg khi tự buff. Có thể xem như kĩ năng bị động), có countdown (tính theo turn khi đến turn sẽ tự buff). Quái vật tự buff sẽ hiển thị icon buff tương ứng. Nếu chọn ô tăng sức mạnh thì khi bắt đầu combat game sẽ tự đếm số turn tương ứng với số đã thiết lập và khi đến lượt sẽ tự buff tấn công và tấn công vào ngay lập tức. (vẫn đang xem sét cách nó sẽ vận hành như nào nên mục này sẽ chưa làm.)