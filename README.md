#移植华为openGauss 支持.net 8.0

参考 https://gitee.com/opengauss/openGauss-connector-adonet

## 1. 安装openGauss

容器安装
https://hub.docker.com/r/opengauss/opengauss

```
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

本地推荐工具 使用
DBeaver
驱动
https://mvnrepository.com/artifact/org.opengauss/opengauss-jdbc
工具链接url
例如：jdbc:postgresql://localhost:5432/test
