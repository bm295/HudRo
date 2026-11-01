# Vector database và semantic search

Module Application/Search định nghĩa ITextEmbedding và IVectorRepository. Infrastructure/Search dùng OpenAI embeddings và Qdrant REST API. Tài liệu và câu hỏi dùng cùng model/dimensions; Qdrant so sánh cosine và trả top-K cùng payload nguồn. Không cần gọi chat completion để tìm kiếm.

## Chạy

Cần .NET 10, Docker Compose và API key có quyền embeddings:

```bash
docker compose -f compose.search.yml up -d
export Search__Enabled=true
export Search__ApiKey='your-key'
dotnet run
```

Cấu hình tùy chọn: Search__QdrantUrl (mặc định http://localhost:6333/), Search__QdrantApiKey, Search__Collection, Search__Model (text-embedding-3-small), Search__Dimensions (1536). Collection tự tạo và kiểm tra schema trước mỗi thao tác. Đổi model hoặc dimensions phải đổi collection và index lại toàn bộ; cùng dimensions không đảm bảo cùng không gian ngữ nghĩa. Search mặc định tắt, độc lập Assistant:Mode.

```bash
curl -X PUT http://localhost:5058/api/search/documents/0b5cf8ca-9839-4af0-94c9-d50f5c078770 \
  -H 'Content-Type: application/json' \
  -d '{"text":"Nhà hàng có salad rau củ dành cho khách ăn chay.","source":"menu/vegetarian"}'

curl -X POST http://localhost:5058/api/search/query \
  -H 'Content-Type: application/json' \
  -d '{"query":"Có món nào không dùng thịt?","topK":5,"minScore":0.2}'
```

PUT cùng UUID thay thế tài liệu và vector (idempotent về dữ liệu; mỗi lần vẫn tính embedding). Mỗi bản ghi là một đoạn tối đa 2000 ký tự; chia tài liệu dài trước khi gửi, đặt UUID ổn định riêng cho từng đoạn và source chứa tên tài liệu/vị trí. Chưa có upload PDF, chunking tự động hoặc xóa đoạn cũ khi tài liệu bị rút ngắn. Kết quả gồm id, text, source, score; score là độ tương đồng, không phải xác suất đúng. Điều chỉnh ngưỡng trên tập câu hỏi thực tế. Kho rỗng trả mảng rỗng.

Input sai trả 400, provider lỗi trả 502 với thông báo tổng quát, quá thời hạn 60 giây trả 504. Mỗi HTTP call giới hạn 30 giây. Không tự retry request tốn phí. Client hủy request được truyền xuống provider.

Đây là API nội bộ phục vụ học tập theo mô hình hiện tại của repo, chưa có xác thực/phân quyền hay phân tách tenant. Chỉ bật trên mạng tin cậy; cần bổ sung các kiểm soát đó trước khi đưa lên Internet. Compose chỉ mở Qdrant trên loopback và dùng volume bền vững. Text được gửi tới dịch vụ embeddings; không đưa dữ liệu bí mật khi chưa có chính sách phù hợp. Search chưa được nối vào tool get_menu và không thay đổi dữ liệu order/payment.

## Kiểm thử

```bash
dotnet test HudRo.Fnb.sln --configuration Release
```

SemanticSearchTests dùng fake embeddings và HTTP handler, không tốn API key: kiểm tra validation trước gọi provider, metadata, upsert wait=true, query payload và sai dimensions. Để smoke test thực tế: index ví dụ trên, query bằng cách diễn đạt khác, restart Qdrant rồi query lại để kiểm tra persistence; upsert cùng ID và kiểm tra chỉ còn nội dung mới.

Tài liệu API: https://api.qdrant.tech/api-reference/search/query-points và https://developers.openai.com/api/reference/resources/embeddings/methods/create
