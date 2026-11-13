namespace GufaCoding
{
    public enum StageType
    {
        FindBug,  // 点出有 Bug 的那一行
        PickLine  // 从选项里挑出空行该填的代码
    }

    public class Stage
    {
        public StageType Type;
        public string Hint;      // 产品经理/测试的吐槽，也是提示
        public string[] Code;    // 代码行；PickLine 里用 ______ 表示空
        public string[] Options; // PickLine 的选项
        public int Answer;       // FindBug：Bug 行下标；PickLine：正确选项下标
        public string Explain;   // 答对后的讲解
    }

    public class Level
    {
        public string Title;
        public string Demand;       // 产品经理的需求
        public float TimePerStage;  // 每个任务的 Deadline（秒）
        public int Reward;          // 本关工资
        public Stage[] Stages;
    }

    /// <summary>
    /// 第一章：C 语言时代。加关卡直接往数组里加就行，后面可以改成读 JSON / 远程配置。
    /// </summary>
    public static class LevelDatabase
    {
        static Stage FB(string hint, string explain, int bugLine, params string[] code)
        {
            return new Stage { Type = StageType.FindBug, Hint = hint, Explain = explain, Answer = bugLine, Code = code };
        }

        static Stage PL(string hint, string explain, int answer, string[] code, params string[] options)
        {
            return new Stage { Type = StageType.PickLine, Hint = hint, Explain = explain, Answer = answer, Code = code, Options = options };
        }

        public static readonly Level[] Levels =
        {
            new Level
            {
                Title = "Hello World", TimePerStage = 30, Reward = 100,
                Demand = "做一个跟用户打招呼的功能。\n很简单的，今天下班前上线。",
                Stages = new[]
                {
                    FB("还没上线，编译就过不去。",
                        "第 3 行末尾少了个分号。古法编程第一课：编译器不会帮你猜。", 2,
                        "#include <stdio.h>",
                        "int main() {",
                        "    printf(\"Hello World\\n\")",
                        "    return 0;",
                        "}"),
                    PL("把打招呼的那句补上。",
                        "C 语言用 printf 输出，其他几个是别的语言的写法。", 1,
                        new[] { "#include <stdio.h>", "int main() {", "    ______", "    return 0;", "}" },
                        "console.log(\"你好\");", "printf(\"你好\\n\");", "print(\"你好\")", "System.out.println(\"你好\");"),
                }
            },
            new Level
            {
                Title = "累加器", TimePerStage = 30, Reward = 130,
                Demand = "用户想算 1 加到 n 等于多少。\n别问为什么，做就是了。",
                Stages = new[]
                {
                    FB("测试说：结果每次跑都不一样。",
                        "s 没有初始化，里面是随机的垃圾值。要写 int s = 0;", 1,
                        "int sum(int n) {",
                        "    int s;",
                        "    for (int i = 1; i <= n; i++)",
                        "        s += i;",
                        "    return s;",
                        "}"),
                    PL("从 1 加到 n，一个都不能少。",
                        "要包含 n 本身，所以是 <=。少一个等号，就少加一次。", 2,
                        new[] { "int s = 0;", "______", "    s += i;", "return s;" },
                        "for (int i = 1; i < n; i++)", "for (i in 1..n)", "for (int i = 1; i <= n; i++)", "foreach (int i in n)"),
                }
            },
            new Level
            {
                Title = "登录功能", TimePerStage = 30, Reward = 160,
                Demand = "做个登录，密码对了就让进。\n安全？以后再说。",
                Stages = new[]
                {
                    FB("测试说：随便输什么密码都能登进去。",
                        "= 是赋值，== 才是比较。这样写条件永远为真，谁都能登录。", 1,
                        "int check(int pwd) {",
                        "    if (pwd = 123456) {",
                        "        return 1;",
                        "    }",
                        "    return 0;",
                        "}"),
                    PL("判断用户名是不是 admin。",
                        "C 里比较字符串要用 strcmp，相等时返回 0。直接用 == 比的是地址。", 1,
                        new[] { "int is_admin(char *name) {", "    if (______)", "        return 1;", "    return 0;", "}" },
                        "name == \"admin\"", "strcmp(name, \"admin\") == 0", "name.equals(\"admin\")", "strcmp(name, \"admin\")"),
                }
            },
            new Level
            {
                Title = "清空列表", TimePerStage = 25, Reward = 190,
                Demand = "把用户列表清空。\n对了，偶尔会崩，你顺便看看。",
                Stages = new[]
                {
                    FB("偶尔会崩，偶尔不会。",
                        "数组下标是 0 到 9，<= 10 会写到第 11 个位置，越界了。", 1,
                        "int arr[10];",
                        "for (int i = 0; i <= 10; i++) {",
                        "    arr[i] = 0;",
                        "}"),
                    PL("算出数组里有几个元素。",
                        "sizeof(arr) 是总字节数，要除以单个元素的大小才是个数。", 2,
                        new[] { "int arr[10];", "int n = ______;" },
                        "arr.length", "len(arr)", "sizeof(arr) / sizeof(arr[0])", "sizeof(arr)"),
                }
            },
            new Level
            {
                Title = "读取文件", TimePerStage = 25, Reward = 220,
                Demand = "用户上传的文件读一下。\n服务器内存不够？买就是了。",
                Stages = new[]
                {
                    FB("服务器跑一天，内存就满了。",
                        "malloc 来的内存没有 free，每调用一次就漏 1KB，这就是内存泄漏。return 前要 free(buf);", 3,
                        "void load() {",
                        "    char *buf = malloc(1024);",
                        "    read_data(buf);",
                        "    return;",
                        "}"),
                    PL("malloc 可能失败，检查一下。",
                        "malloc 失败会返回 NULL。注意是 ==，写成 = 又变成赋值了。", 1,
                        new[] { "char *buf = malloc(1024);", "if (______) {", "    return -1;", "}" },
                        "buf = NULL", "buf == NULL", "buf.empty()", "buf is None"),
                }
            },
            new Level
            {
                Title = "指针入门", TimePerStage = 25, Reward = 250,
                Demand = "做个功能，交换两个用户的积分。\n什么是指针？不知道，你看着办。",
                Stages = new[]
                {
                    FB("拿到的数字一会儿是 42，一会儿是乱码。",
                        "n 是局部变量，函数结束就没了。返回它的地址就是野指针。", 2,
                        "int* make_num() {",
                        "    int n = 42;",
                        "    return &n;",
                        "}"),
                    PL("把 a 和 b 指向的值换一下。",
                        "要改的是指针指向的值，所以两边都要带 *。", 0,
                        new[] { "void swap(int *a, int *b) {", "    int t = *a;", "    ______", "    *b = t;", "}" },
                        "*a = *b;", "a = b;", "&a = &b;", "*a = b;"),
                }
            },
            new Level
            {
                Title = "欢迎页", TimePerStage = 25, Reward = 280,
                Demand = "欢迎页显示用户的名字。\n名字能有多长？反正不会太长吧。",
                Stages = new[]
                {
                    FB("名字一长就崩。",
                        "数组只给了 5 个字节，zhangsan 要 9 个（含结尾的 \\0），写爆了。", 1,
                        "void greet() {",
                        "    char name[5];",
                        "    strcpy(name, \"zhangsan\");",
                        "    printf(\"%s\\n\", name);",
                        "}"),
                    PL("把年龄打印出来。",
                        "整数用 %d。用 %s 会把 18 当成地址去读，直接崩。", 2,
                        new[] { "int age = 18;", "printf(\"______\", age);" },
                        "%s\\n", "{age}\\n", "%d\\n", "%f\\n"),
                }
            },
            new Level
            {
                Title = "数数", TimePerStage = 20, Reward = 310,
                Demand = "做个从 0 数到 9 的功能。\n服务器 CPU 怎么 100% 了？",
                Stages = new[]
                {
                    FB("程序跑了一晚上还没停。",
                        "i-- 让 i 越来越小，永远小于 10，死循环了。", 1,
                        "void count() {",
                        "    for (int i = 0; i < 10; i--) {",
                        "        printf(\"%d\\n\", i);",
                        "    }",
                        "}"),
                    PL("按 1 启动，按 2 停止。现在一启动就停。",
                        "不写 break 会一路掉进 case 2，刚启动就停了。", 3,
                        new[] { "switch (cmd) {", "    case 1: start();", "        ______", "    case 2: stop();", "}" },
                        "next;", "goto case 2;", "fallthrough;", "break;"),
                }
            },
            new Level
            {
                Title = "拆分文件", TimePerStage = 20, Reward = 340,
                Demand = "代码太长了，拆成几个文件。\n别问怎么拆，拆就完了。",
                Stages = new[]
                {
                    FB("头文件被重复包含，报了一堆重定义。",
                        "#ifndef 检查的是 UTIL_H，下面却定义了 UTIL_X，防重复包含形同虚设。", 2,
                        "// util.h",
                        "#ifndef UTIL_H",
                        "#define UTIL_X",
                        "int add(int a, int b);",
                        "#endif"),
                    PL("main.c 要用 util.c 里的全局变量。",
                        "extern 告诉编译器：这个变量在别的文件里，链接时再找。", 1,
                        new[] { "// main.c", "______", "int main() {", "    g_count++;", "}" },
                        "global int g_count;", "extern int g_count;", "public int g_count;", "import g_count;"),
                }
            },
            new Level
            {
                Title = "上线前夜", TimePerStage = 20, Reward = 400,
                Demand = "明天发布会，今晚必须上线。\n产品经理已经下班了。",
                Stages = new[]
                {
                    FB("平均分怎么全是整数？",
                        "两个 int 相除是整数除法，小数直接丢了。要写成 (float)total / n，顺便防一下 n 为 0。", 1,
                        "float avg(int total, int n) {",
                        "    return total / n;",
                        "}"),
                    FB("用户退出时，偶尔显示乱码。",
                        "u 已经 free 了，还去读它的 name，这叫释放后使用。先打印，再 free。", 2,
                        "void logout(User *u) {",
                        "    free(u);",
                        "    printf(\"%s 已退出\\n\", u->name);",
                        "}"),
                    PL("最后一行，写完就上线。",
                        "main 返回 0 表示程序正常结束。终于，上线了。", 3,
                        new[] { "int main() {", "    run_server();", "    ______", "}" },
                        "return true;", "exit;", "return \"ok\";", "return 0;"),
                }
            },
        };
    }
}
