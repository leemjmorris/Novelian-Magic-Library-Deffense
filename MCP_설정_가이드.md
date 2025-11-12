# MCP 서버 멀티 컴퓨터 설정 가이드

## 문제 상황
서로 다른 컴퓨터(데스크톱/노트북)에서 Unity MCP를 사용할 때, 사용자 계정명이 달라서 설정 충돌 발생

## 해결 방법: 환경 변수 사용

---

## 1단계: PATH 환경 변수에 uv 추가

### 데스크톱 (happy)
1. `Win + R` → `sysdm.cpl` 입력 → Enter
2. **"고급"** 탭 클릭
3. **"환경 변수"** 버튼 클릭
4. 하단 **"시스템 변수(S)"** 영역에서 `Path` 찾아서 선택
5. **"편집"** 버튼 클릭
6. **"새로 만들기"** 클릭
7. 다음 경로 입력:
   ```
   C:\Users\happy\.local\bin
   ```
8. **"확인"** 버튼 3번 눌러서 모든 창 닫기

### 노트북 (LMJ)
1. `Win + R` → `sysdm.cpl` 입력 → Enter
2. **"고급"** 탭 클릭
3. **"환경 변수"** 버튼 클릭
4. 하단 **"시스템 변수(S)"** 영역에서 `Path` 찾아서 선택
5. **"편집"** 버튼 클릭
6. **"새로 만들기"** 클릭
7. 다음 경로 입력:
   ```
   C:\Users\LMJ\AppData\Local\Microsoft\WinGet\Links
   ```
8. **"확인"** 버튼 3번 눌러서 모든 창 닫기

---

## 2단계: PATH 설정 확인

**새 명령 프롬프트 창**을 열고 (기존 창은 환경 변수 인식 안됨):
```bash
uv --version
```

버전이 정상적으로 출력되면 성공!

---

## 3단계: MCP 설정 파일 수정

### 설정 파일 위치
```
%APPDATA%\Claude\claude_desktop_config.json
```

### 수정할 내용
```json
{
  "servers": {
    "unityMCP": {
      "command": "uv",
      "args": [
        "run",
        "--directory",
        "%LOCALAPPDATA%\\UnityMCP\\UnityMcpServer\\src",
        "server.py"
      ],
      "type": "stdio"
    }
  }
}
```

---

## 4단계: Claude Desktop 재시작

1. Claude Desktop 완전히 종료 (트레이 아이콘에서도 종료)
2. Claude Desktop 다시 실행

---

## 핵심 포인트

- ✅ `%LOCALAPPDATA%`는 Windows가 자동으로 각 사용자 경로로 변환
- ✅ 두 컴퓨터에서 **동일한 설정 파일** 사용 가능
- ✅ Git/클라우드로 설정 동기화 가능
- ✅ 계정명이 달라도 정상 작동

---

## 주의사항

- ⚠️ 새로운 시스템 변수를 만들면 안됨
- ⚠️ 기존 `Path` 변수에 경로만 추가
- ⚠️ 환경 변수 설정 후 새 명령 프롬프트 창에서 확인

---

## 트러블슈팅

### uv --version이 작동하지 않는 경우
1. 경로가 정확한지 확인
2. 명령 프롬프트를 새로 열었는지 확인
3. 컴퓨터 재시작

### MCP 서버가 연결되지 않는 경우
1. Claude Desktop 완전 종료 후 재시작
2. 설정 파일 JSON 문법 오류 확인
3. uv가 정상적으로 실행되는지 확인
