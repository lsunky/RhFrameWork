using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class VersionConfig
{
    /// <summary>
    /// 资源版本号
    /// </summary>
    public int resVersion;

    /// <summary>
    /// 文件md5
    /// </summary>
    public string fileListMd5;

    /// <summary>
    /// 登录地址
    /// </summary>
    public string loginUrl;

    /// <summary>
    /// 包版本号
    /// </summary>
    public string packageVersion;

    /// <summary>
    /// log开关
    /// </summary>
    public bool enableLog;

    public VersionConfig() { }

}