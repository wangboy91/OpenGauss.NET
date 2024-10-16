#移植华为openGauss 支持.net 8.0

参考 
https://gitee.com/opengauss/openGauss-connector-adonet

## 1. 安装openGauss

容器来源

https://hub.docker.com/r/opengauss/opengauss

https://hub.docker.com/r/enmotech/opengauss

可以定义如下docker-compose.yml
```yaml
version: '5.0.0'

services:

  opengauss:
    image: opengauss/opengauss:5.0.0
    restart: always
    ports:
      - 5432:5432
    environment:
      GS_PASSWORD: openGauss@123
    privileged: true
    volumes:
      - ./data/:/var/lib/opengauss/data/

```

## 本地推荐工具 
使用 DBeaver 可以手动下载驱动 
https://mvnrepository.com/artifact/org.opengauss/opengauss-jdbc

驱动名称：GS (随便填)

驱动类型：Generic （不用动）

类名： org.postgresql.Driver 【需要填写正确】

URL模板： jdbc:postgresql://{host}[:{port}]/[{database}]  【需要填写正确】

测试参数链接url

jdbc:postgresql://localhost:5432/postgres
用户名:gaussdb
密码:openGauss@123

## 使用示例

nuget包 WBoy.OpenGauss.NET

```csharp
var connString = "Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase";

await using var conn = new OpenGaussConnection(connString);
await conn.OpenAsync();

// Insert some data
await using (var cmd = new OpenGaussCommand("INSERT INTO data (some_field) VALUES (@p)", conn))
{
    cmd.Parameters.AddWithValue("p", "Hello world");
    await cmd.ExecuteNonQueryAsync();
}

// Retrieve all rows
await using (var cmd = new OpenGaussCommand("SELECT some_field FROM data", conn))
await using (var reader = await cmd.ExecuteReaderAsync())
{
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
}
```
