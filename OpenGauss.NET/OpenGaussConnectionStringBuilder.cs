using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Netstandard20;
using OpenGauss.NET.Replication;

namespace OpenGauss.NET
{
    /// <summary>
    /// Provides a simple way to create and manage the contents of connection strings used by
    /// the <see cref="OpenGaussConnection"/> class.
    /// </summary>
    public sealed partial class OpenGaussConnectionStringBuilder : DbConnectionStringBuilder, IDictionary<string, object?>
    {
        #region Fields

        /// <summary>
        /// Cached DataSource value to reduce allocations on OpenGaussConnection.DataSource.get
        /// </summary>
        string? _dataSourceCached;

        internal string DataSourceCached
            => _dataSourceCached ??= _host is null
                ? string.Empty
                : IsUnixSocket(_host, _port, out var socketPath, replaceForAbstract: false)
                    ? socketPath
                    : $"tcp://{_host}:{_port}";

        TimeSpan? _hostRecheckSecondsTranslated;

        internal TimeSpan HostRecheckSecondsTranslated
            => _hostRecheckSecondsTranslated ??= TimeSpan.FromSeconds(HostRecheckSeconds == 0 ? -1 : HostRecheckSeconds);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the OpenGaussConnectionStringBuilder class.
        /// </summary>
        public OpenGaussConnectionStringBuilder() => Init();

        /// <summary>
        /// Initializes a new instance of the OpenGaussConnectionStringBuilder class, optionally using ODBC rules for quoting values.
        /// </summary>
        /// <param name="useOdbcRules">true to use {} to delimit fields; false to use quotation marks.</param>
        public OpenGaussConnectionStringBuilder(bool useOdbcRules) : base(useOdbcRules) => Init();

        /// <summary>
        /// Initializes a new instance of the OpenGaussConnectionStringBuilder class and sets its <see cref="DbConnectionStringBuilder.ConnectionString"/>.
        /// </summary>
        public OpenGaussConnectionStringBuilder(string? connectionString)
        {
            Init();
            ConnectionString = connectionString;
        }

        // Method fake-returns an int only to make sure it's code-generated
        private partial int Init();

        #endregion

        #region Non-static property handling

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="keyword">The key of the item to get or set.</param>
        /// <returns>The value associated with the specified key.</returns>
        [AllowNull]
        public override object this[string keyword]
        {
            get
            {
                if (!TryGetValue(keyword, out var value))
                    throw new ArgumentException("Keyword not supported: " + keyword, nameof(keyword));
                return value;
            }
            set
            {
                if (value is null)
                {
                    Remove(keyword);
                    return;
                }

                try
                {
                    GeneratedSetter(keyword.ToUpperInvariant(), value);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Couldn't set " + keyword, keyword, e);
                }
            }
        }

        // Method fake-returns an int only to make sure it's code-generated
        private partial int GeneratedSetter(string keyword, object? value);

        object? IDictionary<string, object?>.this[string keyword]
        {
            get => this[keyword];
            set => this[keyword] = value!;
        }

        /// <summary>
        /// Adds an item to the <see cref="OpenGaussConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="item">The key-value pair to be added.</param>
        public void Add(KeyValuePair<string, object?> item)
            => this[item.Key] = item.Value!;

        void IDictionary<string, object?>.Add(string keyword, object? value)
            => this[keyword] = value;

        /// <summary>
        /// Removes the entry with the specified key from the DbConnectionStringBuilder instance.
        /// </summary>
        /// <param name="keyword">The key of the key/value pair to be removed from the connection string in this DbConnectionStringBuilder.</param>
        /// <returns><b>true</b> if the key existed within the connection string and was removed; <b>false</b> if the key did not exist.</returns>
        public override bool Remove(string keyword)
            => RemoveGenerated(keyword.ToUpperInvariant());

        private partial bool RemoveGenerated(string keyword);

        /// <summary>
        /// Removes the entry from the DbConnectionStringBuilder instance.
        /// </summary>
        /// <param name="item">The key/value pair to be removed from the connection string in this DbConnectionStringBuilder.</param>
        /// <returns><b>true</b> if the key existed within the connection string and was removed; <b>false</b> if the key did not exist.</returns>
        public bool Remove(KeyValuePair<string, object?> item)
            => Remove(item.Key);

        /// <summary>
        /// Clears the contents of the <see cref="OpenGaussConnectionStringBuilder"/> instance.
        /// </summary>
        public override void Clear()
        {
            Debug.Assert(Keys != null);
            foreach (var k in Keys.ToArray())
                Remove(k);
        }

        /// <summary>
        /// Determines whether the <see cref="OpenGaussConnectionStringBuilder"/> contains a specific key.
        /// </summary>
        /// <param name="keyword">The key to locate in the <see cref="OpenGaussConnectionStringBuilder"/>.</param>
        /// <returns><b>true</b> if the <see cref="OpenGaussConnectionStringBuilder"/> contains an entry with the specified key; otherwise <b>false</b>.</returns>
        public override bool ContainsKey(string keyword)
            => keyword is null
                ? throw new ArgumentNullException(nameof(keyword))
                : ContainsKeyGenerated(keyword.ToUpperInvariant());

        private partial bool ContainsKeyGenerated(string keyword);

        /// <summary>
        /// Determines whether the <see cref="OpenGaussConnectionStringBuilder"/> contains a specific key-value pair.
        /// </summary>
        /// <param name="item">The item to locate in the <see cref="OpenGaussConnectionStringBuilder"/>.</param>
        /// <returns><b>true</b> if the <see cref="OpenGaussConnectionStringBuilder"/> contains the entry; otherwise <b>false</b>.</returns>
        public bool Contains(KeyValuePair<string, object?> item)
            => TryGetValue(item.Key, out var value) &&
               ((value == null && item.Value == null) || (value != null && value.Equals(item.Value)));

        /// <summary>
        /// Retrieves a value corresponding to the supplied key from this <see cref="OpenGaussConnectionStringBuilder"/>.
        /// </summary>
        /// <param name="keyword">The key of the item to retrieve.</param>
        /// <param name="value">The value corresponding to the key.</param>
        /// <returns><b>true</b> if keyword was found within the connection string, <b>false</b> otherwise.</returns>
        public override bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
        {
            if (keyword == null)
                throw new ArgumentNullException(nameof(keyword));

            return TryGetValueGenerated(keyword.ToUpperInvariant(), out value);
        }

        private partial bool TryGetValueGenerated(string keyword, [NotNullWhen(true)] out object? value);

        void SetValue(string propertyName, object? value)
        {
            var canonicalKeyword = ToCanonicalKeyword(propertyName.ToUpperInvariant());
            if (value == null)
                base.Remove(canonicalKeyword);
            else
                base[canonicalKeyword] = value;
        }

        private partial string ToCanonicalKeyword(string keyword);

        #endregion

        #region Properties - Connection

        /// <summary>
        /// The hostname or IP address of the PostgreSQL server to connect to.
        /// </summary>
        [Category("Connection")]
        [Description("The hostname or IP address of the PostgreSQL server to connect to.")]
        [DisplayName("Host")]
        [OpenGaussConnectionStringProperty("Server")]
        public string? Host
        {
            get => _host;
            set
            {
                _host = value;
                SetValue(nameof(Host), value);
                _dataSourceCached = null;
            }
        }
        string? _host;

        /// <summary>
        /// The TCP/IP port of the PostgreSQL server.
        /// </summary>
        [Category("Connection")]
        [Description("The TCP port of the PostgreSQL server.")]
        [DisplayName("Port")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(OpenGaussConnection.DefaultPort)]
        public int Port
        {
            get => _port;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid port: " + value);

                _port = value;
                SetValue(nameof(Port), value);
                _dataSourceCached = null;
            }
        }
        int _port;

        ///<summary>
        /// The PostgreSQL database to connect to.
        /// </summary>
        [Category("Connection")]
        [Description("The PostgreSQL database to connect to.")]
        [DisplayName("Database")]
        [OpenGaussConnectionStringProperty("DB")]
        public string? Database
        {
            get => _database;
            set
            {
                _database = value;
                SetValue(nameof(Database), value);
            }
        }
        string? _database;

        /// <summary>
        /// The username to connect with. Not required if using IntegratedSecurity.
        /// </summary>
        [Category("Connection")]
        [Description("The username to connect with. Not required if using IntegratedSecurity.")]
        [DisplayName("Username")]
        [OpenGaussConnectionStringProperty("User Name", "UserId", "User Id", "UID")]
        public string? Username
        {
            get => _username;
            set
            {
                _username = value;
                SetValue(nameof(Username), value);
            }
        }
        string? _username;

        /// <summary>
        /// The password to connect with. Not required if using IntegratedSecurity.
        /// </summary>
        [Category("Connection")]
        [Description("The password to connect with. Not required if using IntegratedSecurity.")]
        [PasswordPropertyText(true)]
        [DisplayName("Password")]
        [OpenGaussConnectionStringProperty("PSW", "PWD")]
        public string? Password
        {
            get => _password;
            set
            {
                _password = value;
                SetValue(nameof(Password), value);
            }
        }
        string? _password;

        /// <summary>
        /// Path to a PostgreSQL password file (PGPASSFILE), from which the password would be taken.
        /// </summary>
        [Category("Connection")]
        [Description("Path to a PostgreSQL password file (PGPASSFILE), from which the password would be taken.")]
        [DisplayName("Passfile")]
        [OpenGaussConnectionStringProperty]
        public string? Passfile
        {
            get => _passfile;
            set
            {
                _passfile = value;
                SetValue(nameof(Passfile), value);
            }
        }

        string? _passfile;

        /// <summary>
        /// The optional application name parameter to be sent to the backend during connection initiation.
        /// </summary>
        [Category("Connection")]
        [Description("The optional application name parameter to be sent to the backend during connection initiation")]
        [DisplayName("Application Name")]
        [OpenGaussConnectionStringProperty]
        public string? ApplicationName
        {
            get => _applicationName;
            set
            {
                _applicationName = value;
                SetValue(nameof(ApplicationName), value);
            }
        }
        string? _applicationName;

        /// <summary>
        /// Whether to enlist in an ambient TransactionScope.
        /// </summary>
        [Category("Connection")]
        [Description("Whether to enlist in an ambient TransactionScope.")]
        [DisplayName("Enlist")]
        [DefaultValue(true)]
        [OpenGaussConnectionStringProperty]
        public bool Enlist
        {
            get => _enlist;
            set
            {
                _enlist = value;
                SetValue(nameof(Enlist), value);
            }
        }
        bool _enlist;

        /// <summary>
        /// Gets or sets the schema search path.
        /// </summary>
        [Category("Connection")]
        [Description("Gets or sets the schema search path.")]
        [DisplayName("Search Path")]
        [OpenGaussConnectionStringProperty]
        public string? SearchPath
        {
            get => _searchPath;
            set
            {
                _searchPath = value;
                SetValue(nameof(SearchPath), value);
            }
        }
        string? _searchPath;

        /// <summary>
        /// Gets or sets the client_encoding parameter.
        /// </summary>
        [Category("Connection")]
        [Description("Gets or sets the client_encoding parameter.")]
        [DisplayName("Client Encoding")]
        [OpenGaussConnectionStringProperty]
        public string? ClientEncoding
        {
            get => _clientEncoding;
            set
            {
                _clientEncoding = value;
                SetValue(nameof(ClientEncoding), value);
            }
        }
        string? _clientEncoding;

        /// <summary>
        /// Gets or sets the .NET encoding that will be used to encode/decode PostgreSQL string data.
        /// </summary>
        [Category("Connection")]
        [Description("Gets or sets the .NET encoding that will be used to encode/decode PostgreSQL string data.")]
        [DisplayName("Encoding")]
        [DefaultValue("UTF8")]
        [OpenGaussConnectionStringProperty]
        public string Encoding
        {
            get => _encoding;
            set
            {
                _encoding = value;
                SetValue(nameof(Encoding), value);
            }
        }
        string _encoding = "UTF8";

        /// <summary>
        /// Gets or sets the PostgreSQL session timezone, in Olson/IANA database format.
        /// </summary>
        [Category("Connection")]
        [Description("Gets or sets the PostgreSQL session timezone, in Olson/IANA database format.")]
        [DisplayName("Timezone")]
        [OpenGaussConnectionStringProperty]
        public string? Timezone
        {
            get => _timezone;
            set
            {
                _timezone = value;
                SetValue(nameof(Timezone), value);
            }
        }
        string? _timezone;

        #endregion

        #region Properties - Security

        /// <summary>
        /// Controls whether SSL is required, disabled or preferred, depending on server support.
        /// </summary>
        [Category("Security")]
        [Description("Controls whether SSL is required, disabled or preferred, depending on server support.")]
        [DisplayName("SSL Mode")]
        [DefaultValue(SslMode.Prefer)]
        [OpenGaussConnectionStringProperty]
        public SslMode SslMode
        {
            get => _sslMode;
            set
            {
                _sslMode = value;
                SetValue(nameof(SslMode), value);
            }
        }
        SslMode _sslMode;

        /// <summary>
        /// Whether to trust the server certificate without validating it.
        /// </summary>
        [Category("Security")]
        [Description("Whether to trust the server certificate without validating it.")]
        [DisplayName("Trust Server Certificate")]
        [OpenGaussConnectionStringProperty]
        public bool TrustServerCertificate
        {
            get => _trustServerCertificate;
            set
            {
                _trustServerCertificate = value;
                SetValue(nameof(TrustServerCertificate), value);
            }
        }
        bool _trustServerCertificate;

        /// <summary>
        /// Location of a client certificate to be sent to the server.
        /// </summary>
        [Category("Security")]
        [Description("Location of a client certificate to be sent to the server.")]
        [DisplayName("SSL Certificate")]
        [OpenGaussConnectionStringProperty]
        public string? SslCertificate
        {
            get => _sslCertificate;
            set
            {
                _sslCertificate = value;
                SetValue(nameof(SslCertificate), value);
            }
        }
        string? _sslCertificate;

        /// <summary>
        /// Location of a client key for a client certificate to be sent to the server.
        /// </summary>
        [Category("Security")]
        [Description("Location of a client key for a client certificate to be sent to the server.")]
        [DisplayName("SSL Key")]
        [OpenGaussConnectionStringProperty]
        public string? SslKey
        {
            get => _sslKey;
            set
            {
                _sslKey = value;
                SetValue(nameof(SslKey), value);
            }
        }
        string? _sslKey;

        /// <summary>
        /// Password for a key for a client certificate.
        /// </summary>
        [Category("Security")]
        [Description("Password for a key for a client certificate.")]
        [DisplayName("SSL Password")]
        [OpenGaussConnectionStringProperty]
        public string? SslPassword
        {
            get => _sslPassword;
            set
            {
                _sslPassword = value;
                SetValue(nameof(SslPassword), value);
            }
        }
        string? _sslPassword;

        /// <summary>
        /// Location of a CA certificate used to validate the server certificate.
        /// </summary>
        [Category("Security")]
        [Description("Location of a CA certificate used to validate the server certificate.")]
        [DisplayName("Root Certificate")]
        [OpenGaussConnectionStringProperty]
        public string? RootCertificate
        {
            get => _rootCertificate;
            set
            {
                _rootCertificate = value;
                SetValue(nameof(RootCertificate), value);
            }
        }
        string? _rootCertificate;

        /// <summary>
        /// Whether to check the certificate revocation list during authentication.
        /// False by default.
        /// </summary>
        [Category("Security")]
        [Description("Whether to check the certificate revocation list during authentication.")]
        [DisplayName("Check Certificate Revocation")]
        [OpenGaussConnectionStringProperty]
        public bool CheckCertificateRevocation
        {
            get => _checkCertificateRevocation;
            set
            {
                _checkCertificateRevocation = value;
                SetValue(nameof(CheckCertificateRevocation), value);
            }
        }
        bool _checkCertificateRevocation;

        /// <summary>
        /// Whether to use Windows integrated security to log in.
        /// </summary>
        [Category("Security")]
        [Description("Whether to use Windows integrated security to log in.")]
        [DisplayName("Integrated Security")]
        [OpenGaussConnectionStringProperty]
        public bool IntegratedSecurity
        {
            get => _integratedSecurity;
            set
            {
                // No integrated security if we're on mono and .NET 4.5 because of ClaimsIdentity,
                // see https://github.com/opengauss/OpenGauss/issues/133
                if (value && Type.GetType("Mono.Runtime") != null)
                    throw new NotSupportedException("IntegratedSecurity is currently unsupported on mono and .NET 4.5 (see https://github.com/opengauss/OpenGauss/issues/133)");
                _integratedSecurity = value;
                SetValue(nameof(IntegratedSecurity), value);
            }
        }
        bool _integratedSecurity;

        /// <summary>
        /// The Kerberos service name to be used for authentication.
        /// </summary>
        [Category("Security")]
        [Description("The Kerberos service name to be used for authentication.")]
        [DisplayName("Kerberos Service Name")]
        [OpenGaussConnectionStringProperty("Krbsrvname")]
        [DefaultValue("postgres")]
        public string KerberosServiceName
        {
            get => _kerberosServiceName;
            set
            {
                _kerberosServiceName = value;
                SetValue(nameof(KerberosServiceName), value);
            }
        }
        string _kerberosServiceName = "postgres";

        /// <summary>
        /// The Kerberos realm to be used for authentication.
        /// </summary>
        [Category("Security")]
        [Description("The Kerberos realm to be used for authentication.")]
        [DisplayName("Include Realm")]
        [OpenGaussConnectionStringProperty]
        public bool IncludeRealm
        {
            get => _includeRealm;
            set
            {
                _includeRealm = value;
                SetValue(nameof(IncludeRealm), value);
            }
        }
        bool _includeRealm;

        /// <summary>
        /// Gets or sets a Boolean value that indicates if security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state.
        /// </summary>
        [Category("Security")]
        [Description("Gets or sets a Boolean value that indicates if security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state.")]
        [DisplayName("Persist Security Info")]
        [OpenGaussConnectionStringProperty]
        public bool PersistSecurityInfo
        {
            get => _persistSecurityInfo;
            set
            {
                _persistSecurityInfo = value;
                SetValue(nameof(PersistSecurityInfo), value);
            }
        }
        bool _persistSecurityInfo;

        /// <summary>
        /// When enabled, parameter values are logged when commands are executed. Defaults to false.
        /// </summary>
        [Category("Security")]
        [Description("When enabled, parameter values are logged when commands are executed. Defaults to false.")]
        [DisplayName("Log Parameters")]
        [OpenGaussConnectionStringProperty]
        public bool LogParameters
        {
            get => _logParameters;
            set
            {
                _logParameters = value;
                SetValue(nameof(LogParameters), value);
            }
        }
        bool _logParameters;

        internal const string IncludeExceptionDetailDisplayName = "Include Error Detail";

        /// <summary>
        /// When enabled, PostgreSQL error details are included on <see cref="PostgresException.Detail" /> and
        /// <see cref="PostgresNotice.Detail" />. These can contain sensitive data.
        /// </summary>
        [Category("Security")]
        [Description("When enabled, PostgreSQL error and notice details are included on PostgresException.Detail and PostgresNotice.Detail. These can contain sensitive data.")]
        [DisplayName(IncludeExceptionDetailDisplayName)]
        [OpenGaussConnectionStringProperty]
        public bool IncludeErrorDetail
        {
            get => _includeErrorDetail;
            set
            {
                _includeErrorDetail = value;
                SetValue(nameof(IncludeErrorDetail), value);
            }
        }
        bool _includeErrorDetail;

        #endregion

        #region Properties - Pooling

        /// <summary>
        /// Whether connection pooling should be used.
        /// </summary>
        [Category("Pooling")]
        [Description("Whether connection pooling should be used.")]
        [DisplayName("Pooling")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(true)]
        public bool Pooling
        {
            get => _pooling;
            set
            {
                _pooling = value;
                SetValue(nameof(Pooling), value);
            }
        }
        bool _pooling;

        /// <summary>
        /// The minimum connection pool size.
        /// </summary>
        [Category("Pooling")]
        [Description("The minimum connection pool size.")]
        [DisplayName("Minimum Pool Size")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(0)]
        public int MinPoolSize
        {
            get => _minPoolSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MinPoolSize can't be negative");

                _minPoolSize = value;
                SetValue(nameof(MinPoolSize), value);
            }
        }
        int _minPoolSize;

        /// <summary>
        /// The maximum connection pool size.
        /// </summary>
        [Category("Pooling")]
        [Description("The maximum connection pool size.")]
        [DisplayName("Maximum Pool Size")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(100)]
        public int MaxPoolSize
        {
            get => _maxPoolSize;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxPoolSize can't be negative");

                _maxPoolSize = value;
                SetValue(nameof(MaxPoolSize), value);
            }
        }
        int _maxPoolSize;

        /// <summary>
        /// The time to wait before closing idle connections in the pool if the count
        /// of all connections exceeds MinPoolSize.
        /// </summary>
        /// <value>The time (in seconds) to wait. The default value is 300.</value>
        [Category("Pooling")]
        [Description("The time to wait before closing unused connections in the pool if the count of all connections exceeds MinPoolSize.")]
        [DisplayName("Connection Idle Lifetime")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(300)]
        public int ConnectionIdleLifetime
        {
            get => _connectionIdleLifetime;
            set
            {
                _connectionIdleLifetime = value;
                SetValue(nameof(ConnectionIdleLifetime), value);
            }
        }
        int _connectionIdleLifetime;

        /// <summary>
        /// How many seconds the pool waits before attempting to prune idle connections that are beyond
        /// idle lifetime (<see cref="ConnectionIdleLifetime"/>.
        /// </summary>
        /// <value>The interval (in seconds). The default value is 10.</value>
        [Category("Pooling")]
        [Description("How many seconds the pool waits before attempting to prune idle connections that are beyond idle lifetime.")]
        [DisplayName("Connection Pruning Interval")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(10)]
        public int ConnectionPruningInterval
        {
            get => _connectionPruningInterval;
            set
            {
                _connectionPruningInterval = value;
                SetValue(nameof(ConnectionPruningInterval), value);
            }
        }
        int _connectionPruningInterval;

        /// <summary>
        /// The total maximum lifetime of connections (in seconds). Connections which have exceeded this value will be
        /// destroyed instead of returned from the pool. This is useful in clustered configurations to force load
        /// balancing between a running server and a server just brought online.
        /// </summary>
        /// <value>The time (in seconds) to wait, or 0 to to make connections last indefinitely (the default).</value>
        [Category("Pooling")]
        [Description("The total maximum lifetime of connections (in seconds).")]
        [DisplayName("Connection Lifetime")]
        [OpenGaussConnectionStringProperty("Load Balance Timeout")]
        public int ConnectionLifetime
        {
            get => _connectionLifetime;
            set
            {
                _connectionLifetime = value;
                SetValue(nameof(ConnectionLifetime), value);
            }
        }
        int _connectionLifetime;

        #endregion

        #region Properties - Timeouts

        /// <summary>
        /// The time to wait (in seconds) while trying to establish a connection before terminating the attempt and generating an error.
        /// Defaults to 15 seconds.
        /// </summary>
        [Category("Timeouts")]
        [Description("The time to wait (in seconds) while trying to establish a connection before terminating the attempt and generating an error.")]
        [DisplayName("Timeout")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(DefaultTimeout)]
        public int Timeout
        {
            get => _timeout;
            set
            {
                if (value < 0 || value > OpenGaussConnection.TimeoutLimit)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Timeout must be between 0 and " + OpenGaussConnection.TimeoutLimit);

                _timeout = value;
                SetValue(nameof(Timeout), value);
            }
        }
        int _timeout;

        internal const int DefaultTimeout = 15;

        /// <summary>
        /// The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error.
        /// Defaults to 30 seconds.
        /// </summary>
        [Category("Timeouts")]
        [Description("The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error. Set to zero for infinity.")]
        [DisplayName("Command Timeout")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(OpenGaussCommand.DefaultTimeout)]
        public int CommandTimeout
        {
            get => _commandTimeout;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "CommandTimeout can't be negative");

                _commandTimeout = value;
                SetValue(nameof(CommandTimeout), value);
            }
        }
        int _commandTimeout;

        /// <summary>
        /// The time to wait (in seconds) while trying to execute a an internal command before terminating the attempt and generating an error.
        /// </summary>
        [Category("Timeouts")]
        [Description("The time to wait (in seconds) while trying to execute a an internal command before terminating the attempt and generating an error. -1 uses CommandTimeout, 0 means no timeout.")]
        [DisplayName("Internal Command Timeout")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(-1)]
        public int InternalCommandTimeout
        {
            get => _internalCommandTimeout;
            set
            {
                if (value != 0 && value != -1 && value < OpenGaussConnector.MinimumInternalCommandTimeout)
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        $"InternalCommandTimeout must be >= {OpenGaussConnector.MinimumInternalCommandTimeout}, 0 (infinite) or -1 (use CommandTimeout)");

                _internalCommandTimeout = value;
                SetValue(nameof(InternalCommandTimeout), value);
            }
        }
        int _internalCommandTimeout;

        /// <summary>
        /// The time to wait (in milliseconds) while trying to read a response for a cancellation request for a timed out or cancelled query, before terminating the attempt and generating an error.
        /// Zero for infinity, -1 to skip the wait.
        /// Defaults to 2000 milliseconds.
        /// </summary>
        [Category("Timeouts")]
        [Description("After Command Timeout is reached (or user supplied cancellation token is cancelled) and command cancellation is attempted, OpenGauss waits for this additional timeout (in milliseconds) before breaking the connection. Defaults to 2000, set to zero for infinity.")]
        [DisplayName("Cancellation Timeout")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(2000)]
        public int CancellationTimeout
        {
            get => _cancellationTimeout;
            set
            {
                if (value < -1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(CancellationTimeout)} can't less than -1");

                _cancellationTimeout = value;
                SetValue(nameof(CancellationTimeout), value);
            }
        }
        int _cancellationTimeout;

        #endregion

        #region Properties - Failover and load balancing

        /// <summary>
        /// Determines the preferred PostgreSQL target server type.
        /// </summary>
        [Category("Failover and load balancing")]
        [Description("Determines the preferred PostgreSQL target server type.")]
        [DisplayName("Target Session Attributes")]
        [OpenGaussConnectionStringProperty]
        public string? TargetSessionAttributes
        {
            get => TargetSessionAttributesParsed switch
            {
                NET.TargetSessionAttributes.Any           => "any",
                NET.TargetSessionAttributes.Primary       => "primary",
                NET.TargetSessionAttributes.Standby       => "standby",
                NET.TargetSessionAttributes.PreferPrimary => "prefer-primary",
                NET.TargetSessionAttributes.PreferStandby => "prefer-standby",
                NET.TargetSessionAttributes.ReadWrite     => "read-write",
                NET.TargetSessionAttributes.ReadOnly      => "read-only",
                null => null,

                _ => throw new ArgumentException($"Unhandled enum value '{TargetSessionAttributesParsed}'")
            };

            set
            {
                TargetSessionAttributesParsed = value is null ? null : ParseTargetSessionAttributes(value);
                SetValue(nameof(TargetSessionAttributes), value);
            }
        }

        internal TargetSessionAttributes? TargetSessionAttributesParsed { get; private set; }

        internal static TargetSessionAttributes ParseTargetSessionAttributes(string s)
            => s switch
            {
                "any"            => NET.TargetSessionAttributes.Any,
                "primary"        => NET.TargetSessionAttributes.Primary,
                "standby"        => NET.TargetSessionAttributes.Standby,
                "prefer-primary" => NET.TargetSessionAttributes.PreferPrimary,
                "prefer-standby" => NET.TargetSessionAttributes.PreferStandby,
                "read-write"     => NET.TargetSessionAttributes.ReadWrite,
                "read-only"      => NET.TargetSessionAttributes.ReadOnly,

                _ => throw new ArgumentException($"TargetSessionAttributes contains an invalid value '{s}'")
            };

        /// <summary>
        /// Enables balancing between multiple hosts by round-robin.
        /// </summary>
        [Category("Failover and load balancing")]
        [Description("Enables balancing between multiple hosts by round-robin.")]
        [DisplayName("Load Balance Hosts")]
        [OpenGaussConnectionStringProperty]
        public bool LoadBalanceHosts
        {
            get => _loadBalanceHosts;
            set
            {
                _loadBalanceHosts = value;
                SetValue(nameof(LoadBalanceHosts), value);
            }
        }
        bool _loadBalanceHosts;

        /// <summary>
        /// Controls for how long the host's cached state will be considered as valid.
        /// </summary>
        [Category("Failover and load balancing")]
        [Description("Controls for how long the host's cached state will be considered as valid.")]
        [DisplayName("Host Recheck Seconds")]
        [DefaultValue(10)]
        [OpenGaussConnectionStringProperty]
        public int HostRecheckSeconds
        {
            get => _hostRecheckSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentException($"{HostRecheckSeconds} cannot be negative", nameof(HostRecheckSeconds));
                _hostRecheckSeconds = value;
                SetValue(nameof(HostRecheckSeconds), value);
                _hostRecheckSecondsTranslated = null;
            }
        }
        int _hostRecheckSeconds;

        #endregion Properties - Failover and load balancing

        #region Properties - Entity Framework

        /// <summary>
        /// The database template to specify when creating a database in Entity Framework. If not specified,
        /// PostgreSQL defaults to "template1".
        /// </summary>
        /// <remarks>
        /// https://www.postgresql.org/docs/current/static/manage-ag-templatedbs.html
        /// </remarks>
        [Category("Entity Framework")]
        [Description("The database template to specify when creating a database in Entity Framework. If not specified, PostgreSQL defaults to \"template1\".")]
        [DisplayName("EF Template Database")]
        [OpenGaussConnectionStringProperty]
        public string? EntityTemplateDatabase
        {
            get => _entityTemplateDatabase;
            set
            {
                _entityTemplateDatabase = value;
                SetValue(nameof(EntityTemplateDatabase), value);
            }
        }
        string? _entityTemplateDatabase;

        /// <summary>
        /// The database admin to specify when creating and dropping a database in Entity Framework. This is needed because
        /// OpenGauss needs to connect to a database in order to send the create/drop database command.
        /// If not specified, defaults to "template1". Check OpenGaussServices.UsingPostgresDBConnection for more information.
        /// </summary>
        [Category("Entity Framework")]
        [Description("The database admin to specify when creating and dropping a database in Entity Framework. If not specified, defaults to \"template1\".")]
        [DisplayName("EF Admin Database")]
        [OpenGaussConnectionStringProperty]
        public string? EntityAdminDatabase
        {
            get => _entityAdminDatabase;
            set
            {
                _entityAdminDatabase = value;
                SetValue(nameof(EntityAdminDatabase), value);
            }
        }
        string? _entityAdminDatabase;

        #endregion

        #region Properties - Advanced

        /// <summary>
        /// The number of seconds of connection inactivity before OpenGauss sends a keepalive query.
        /// Set to 0 (the default) to disable.
        /// </summary>
        [Category("Advanced")]
        [Description("The number of seconds of connection inactivity before OpenGauss sends a keepalive query.")]
        [DisplayName("Keepalive")]
        [OpenGaussConnectionStringProperty]
        public int KeepAlive
        {
            get => _keepAlive;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "KeepAlive can't be negative");

                _keepAlive = value;
                SetValue(nameof(KeepAlive), value);
            }
        }
        int _keepAlive;

        /// <summary>
        /// Whether to use TCP keepalive with system defaults if overrides isn't specified.
        /// </summary>
        [Category("Advanced")]
        [Description("Whether to use TCP keepalive with system defaults if overrides isn't specified.")]
        [DisplayName("TCP Keepalive")]
        [OpenGaussConnectionStringProperty]
        public bool TcpKeepAlive
        {
            get => _tcpKeepAlive;
            set
            {
                _tcpKeepAlive = value;
                SetValue(nameof(TcpKeepAlive), value);
            }
        }
        bool _tcpKeepAlive;

        /// <summary>
        /// The number of seconds of connection inactivity before a TCP keepalive query is sent.
        /// Use of this option is discouraged, use <see cref="KeepAlive"/> instead if possible.
        /// Set to 0 (the default) to disable.
        /// </summary>
        [Category("Advanced")]
        [Description("The number of seconds of connection inactivity before a TCP keepalive query is sent.")]
        [DisplayName("TCP Keepalive Time")]
        [OpenGaussConnectionStringProperty]
        public int TcpKeepAliveTime
        {
            get => _tcpKeepAliveTime;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "TcpKeepAliveTime can't be negative");

                _tcpKeepAliveTime = value;
                SetValue(nameof(TcpKeepAliveTime), value);
            }
        }
        int _tcpKeepAliveTime;

        /// <summary>
        /// The interval, in seconds, between when successive keep-alive packets are sent if no acknowledgement is received.
        /// Defaults to the value of <see cref="TcpKeepAliveTime"/>. <see cref="TcpKeepAliveTime"/> must be non-zero as well.
        /// </summary>
        [Category("Advanced")]
        [Description("The interval, in seconds, between when successive keep-alive packets are sent if no acknowledgement is received.")]
        [DisplayName("TCP Keepalive Interval")]
        [OpenGaussConnectionStringProperty]
        public int TcpKeepAliveInterval
        {
            get => _tcpKeepAliveInterval;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "TcpKeepAliveInterval can't be negative");

                _tcpKeepAliveInterval = value;
                SetValue(nameof(TcpKeepAliveInterval), value);
            }
        }
        int _tcpKeepAliveInterval;

        /// <summary>
        /// Determines the size of the internal buffer OpenGauss uses when reading. Increasing may improve performance if transferring large values from the database.
        /// </summary>
        [Category("Advanced")]
        [Description("Determines the size of the internal buffer OpenGauss uses when reading. Increasing may improve performance if transferring large values from the database.")]
        [DisplayName("Read Buffer Size")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(OpenGaussReadBuffer.DefaultSize)]
        public int ReadBufferSize
        {
            get => _readBufferSize;
            set
            {
                _readBufferSize = value;
                SetValue(nameof(ReadBufferSize), value);
            }
        }
        int _readBufferSize;

        /// <summary>
        /// Determines the size of the internal buffer OpenGauss uses when writing. Increasing may improve performance if transferring large values to the database.
        /// </summary>
        [Category("Advanced")]
        [Description("Determines the size of the internal buffer OpenGauss uses when writing. Increasing may improve performance if transferring large values to the database.")]
        [DisplayName("Write Buffer Size")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(OpenGaussWriteBuffer.DefaultSize)]
        public int WriteBufferSize
        {
            get => _writeBufferSize;
            set
            {
                _writeBufferSize = value;
                SetValue(nameof(WriteBufferSize), value);
            }
        }
        int _writeBufferSize;

        /// <summary>
        /// Determines the size of socket read buffer.
        /// </summary>
        [Category("Advanced")]
        [Description("Determines the size of socket receive buffer.")]
        [DisplayName("Socket Receive Buffer Size")]
        [OpenGaussConnectionStringProperty]
        public int SocketReceiveBufferSize
        {
            get => _socketReceiveBufferSize;
            set
            {
                _socketReceiveBufferSize = value;
                SetValue(nameof(SocketReceiveBufferSize), value);
            }
        }
        int _socketReceiveBufferSize;

        /// <summary>
        /// Determines the size of socket send buffer.
        /// </summary>
        [Category("Advanced")]
        [Description("Determines the size of socket send buffer.")]
        [DisplayName("Socket Send Buffer Size")]
        [OpenGaussConnectionStringProperty]
        public int SocketSendBufferSize
        {
            get => _socketSendBufferSize;
            set
            {
                _socketSendBufferSize = value;
                SetValue(nameof(SocketSendBufferSize), value);
            }
        }
        int _socketSendBufferSize;

        /// <summary>
        /// The maximum number SQL statements that can be automatically prepared at any given point.
        /// Beyond this number the least-recently-used statement will be recycled.
        /// Zero (the default) disables automatic preparation.
        /// </summary>
        [Category("Advanced")]
        [Description("The maximum number SQL statements that can be automatically prepared at any given point. Beyond this number the least-recently-used statement will be recycled. Zero (the default) disables automatic preparation.")]
        [DisplayName("Max Auto Prepare")]
        [OpenGaussConnectionStringProperty]
        public int MaxAutoPrepare
        {
            get => _maxAutoPrepare;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(MaxAutoPrepare)} cannot be negative");

                _maxAutoPrepare = value;
                SetValue(nameof(MaxAutoPrepare), value);
            }
        }
        int _maxAutoPrepare;

        /// <summary>
        /// The minimum number of usages an SQL statement is used before it's automatically prepared.
        /// Defaults to 5.
        /// </summary>
        [Category("Advanced")]
        [Description("The minimum number of usages an SQL statement is used before it's automatically prepared. Defaults to 5.")]
        [DisplayName("Auto Prepare Min Usages")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(5)]
        public int AutoPrepareMinUsages
        {
            get => _autoPrepareMinUsages;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(AutoPrepareMinUsages)} must be 1 or greater");

                _autoPrepareMinUsages = value;
                SetValue(nameof(AutoPrepareMinUsages), value);
            }
        }
        int _autoPrepareMinUsages;

        /// <summary>
        /// If set to true, a pool connection's state won't be reset when it is closed (improves performance).
        /// Do not specify this unless you know what you're doing.
        /// </summary>
        [Category("Advanced")]
        [Description("If set to true, a pool connection's state won't be reset when it is closed (improves performance). Do not specify this unless you know what you're doing.")]
        [DisplayName("No Reset On Close")]
        [OpenGaussConnectionStringProperty]
        public bool NoResetOnClose
        {
            get => _noResetOnClose;
            set
            {
                _noResetOnClose = value;
                SetValue(nameof(NoResetOnClose), value);
            }
        }
        bool _noResetOnClose;

        /// <summary>
        /// Load table composite type definitions, and not just free-standing composite types.
        /// </summary>
        [Category("Advanced")]
        [Description("Load table composite type definitions, and not just free-standing composite types.")]
        [DisplayName("Load Table Composites")]
        [OpenGaussConnectionStringProperty]
        public bool LoadTableComposites
        {
            get => _loadTableComposites;
            set
            {
                _loadTableComposites = value;
                SetValue(nameof(LoadTableComposites), value);
            }
        }
        bool _loadTableComposites;

        /// <summary>
        /// Set the replication mode of the connection
        /// </summary>
        /// <remarks>
        /// This property and its corresponding enum are intentionally kept internal as they
        /// should not be set by users or even be visible in their connection strings.
        /// Replication connections are a special kind of connection that is encapsulated in
        /// <see cref="PhysicalReplicationConnection"/>
        /// and <see cref="LogicalReplicationConnection"/>.
        /// </remarks>
        [OpenGaussConnectionStringProperty]
        [DisplayName("Replication Mode")]
        internal ReplicationMode ReplicationMode
        {
            get => _replicationMode;
            set
            {
                _replicationMode = value;
                SetValue(nameof(ReplicationMode), value);
            }
        }
        ReplicationMode _replicationMode;

        /// <summary>
        /// Set PostgreSQL configuration parameter default values for the connection.
        /// </summary>
        [Category("Advanced")]
        [Description("Set PostgreSQL configuration parameter default values for the connection.")]
        [DisplayName("Options")]
        [OpenGaussConnectionStringProperty]
        public string? Options
        {
            get => _options;
            set
            {
                _options = value;
                SetValue(nameof(Options), value);
            }
        }

        string? _options;

        /// <summary>
        /// Configure the way arrays of value types are returned when requested as object instances.
        /// </summary>
        [Category("Advanced")]
        [Description("Configure the way arrays of value types are returned when requested as object instances.")]
        [DisplayName("Array Nullability Mode")]
        [OpenGaussConnectionStringProperty]
        public ArrayNullabilityMode ArrayNullabilityMode
        {
            get => _arrayNullabilityMode;
            set
            {
                _arrayNullabilityMode = value;
                SetValue(nameof(ArrayNullabilityMode), value);
            }
        }

        ArrayNullabilityMode _arrayNullabilityMode;

        #endregion

        #region Multiplexing

        /// <summary>
        /// Enables multiplexing, which allows more efficient use of connections.
        /// </summary>
        [Category("Multiplexing")]
        [Description("Enables multiplexing, which allows more efficient use of connections.")]
        [DisplayName("Multiplexing")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(false)]
        public bool Multiplexing
        {
            get => _multiplexing;
            set
            {
                _multiplexing = value;
                SetValue(nameof(Multiplexing), value);
            }
        }
        bool _multiplexing;

        /// <summary>
        /// When multiplexing is enabled, determines the maximum number of outgoing bytes to buffer before
        /// flushing to the network.
        /// </summary>
        [Category("Multiplexing")]
        [Description("When multiplexing is enabled, determines the maximum number of outgoing bytes to buffer before " +
                     "flushing to the network.")]
        [DisplayName("Write Coalescing Buffer Threshold Bytes")]
        [OpenGaussConnectionStringProperty]
        [DefaultValue(1000)]
        public int WriteCoalescingBufferThresholdBytes
        {
            get => _writeCoalescingBufferThresholdBytes;
            set
            {
                _writeCoalescingBufferThresholdBytes = value;
                SetValue(nameof(WriteCoalescingBufferThresholdBytes), value);
            }
        }
        int _writeCoalescingBufferThresholdBytes;

        #endregion

        #region Properties - Compatibility

        /// <summary>
        /// A compatibility mode for special PostgreSQL server types.
        /// </summary>
        [Category("Compatibility")]
        [Description("A compatibility mode for special PostgreSQL server types.")]
        [DisplayName("Server Compatibility Mode")]
        [OpenGaussConnectionStringProperty]
        public ServerCompatibilityMode ServerCompatibilityMode
        {
            get => _serverCompatibilityMode;
            set
            {
                _serverCompatibilityMode = value;
                SetValue(nameof(ServerCompatibilityMode), value);
            }
        }
        ServerCompatibilityMode _serverCompatibilityMode;

        #endregion

        #region Properties - Obsolete

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/6.0.html
        /// </summary>
        [Category("Compatibility")]
        [Description("Makes MaxValue and MinValue timestamps and dates readable as infinity and negative infinity.")]
        [DisplayName("Convert Infinity DateTime")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The ConvertInfinityDateTime parameter is no longer supported.")]
        public bool ConvertInfinityDateTime
        {
            get => false;
            set => throw new NotSupportedException("The Convert Infinity DateTime parameter is no longer supported; OpenGauss 6.0 and above convert min/max values to Infinity by default. See https://www.opengauss.org/doc/types/datetime.html for more details.");
        }

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/3.1.html
        /// </summary>
        [Category("Obsolete")]
        [Description("Obsolete, see https://www.opengauss.org/doc/release-notes/3.1.html")]
        [DisplayName("Continuous Processing")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The ContinuousProcessing parameter is no longer supported.")]
        public bool ContinuousProcessing
        {
            get => false;
            set => throw new NotSupportedException("The ContinuousProcessing parameter is no longer supported. Please see https://www.opengauss.org/doc/release-notes/3.1.html");
        }

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/3.1.html
        /// </summary>
        [Category("Obsolete")]
        [Description("Obsolete, see https://www.opengauss.org/doc/release-notes/3.1.html")]
        [DisplayName("Backend Timeouts")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The BackendTimeouts parameter is no longer supported")]
        public bool BackendTimeouts
        {
            get => false;
            set => throw new NotSupportedException("The BackendTimeouts parameter is no longer supported. Please see https://www.opengauss.org/doc/release-notes/3.1.html");
        }

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/3.0.html
        /// </summary>
        [Category("Obsolete")]
        [Description("Obsolete, see https://www.opengauss.org/doc/v/3.0.html")]
        [DisplayName("Preload Reader")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The PreloadReader parameter is no longer supported")]
        public bool PreloadReader
        {
            get => false;
            set => throw new NotSupportedException("The PreloadReader parameter is no longer supported. Please see https://www.opengauss.org/doc/release-notes/3.0.html");
        }

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/3.0.html
        /// </summary>
        [Category("Obsolete")]
        [Description("Obsolete, see https://www.opengauss.org/doc/release-notes/3.0.html")]
        [DisplayName("Use Extended Types")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The UseExtendedTypes parameter is no longer supported")]
        public bool UseExtendedTypes
        {
            get => false;
            set => throw new NotSupportedException("The UseExtendedTypes parameter is no longer supported. Please see https://www.opengauss.org/doc/release-notes/3.0.html");
        }

        /// <summary>
        /// Obsolete, see https://www.opengauss.org/doc/release-notes/4.1.html
        /// </summary>
        [Category("Obsolete")]
        [Description("Obsolete, see https://www.opengauss.org/doc/release-notes/4.1.html")]
        [DisplayName("Use Ssl Stream")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The UseSslStream parameter is no longer supported (always true)")]
        public bool UseSslStream
        {
            get => true;
            set => throw new NotSupportedException("The UseSslStream parameter is no longer supported (SslStream is always used). Please see https://www.opengauss.org/doc/release-notes/4.1.html");
        }

        /// <summary>
        /// Writes connection performance information to performance counters.
        /// </summary>
        [Category("Obsolete")]
        [Description("Writes connection performance information to performance counters.")]
        [DisplayName("Use Perf Counters")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("The UsePerfCounters parameter is no longer supported")]
        public bool UsePerfCounters
        {
            get => false;
            set => throw new NotSupportedException("The UsePerfCounters parameter is no longer supported. Please see https://www.opengauss.org/doc/release-notes/5.0.html");
        }

        /// <summary>
        /// Location of a client certificate to be sent to the server.
        /// </summary>
        [Category("Obsolete")]
        [Description("Location of a client certificate to be sent to the server.")]
        [DisplayName("Client Certificate")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("Use OpenGaussConnectionStringBuilder.SslKey instead")]
        public string? ClientCertificate
        {
            get => SslKey;
            set => SslKey = value;
        }

        /// <summary>
        /// Key for a client certificate to be sent to the server.
        /// </summary>
        [Category("Obsolete")]
        [Description("Key for a client certificate to be sent to the server.")]
        [DisplayName("Client Certificate Key")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("Use OpenGaussConnectionStringBuilder.SslPassword instead")]
        public string? ClientCertificateKey
        {
            get => SslPassword;
            set => SslPassword = value;
        }

        /// <summary>
        /// When enabled, PostgreSQL error details are included on <see cref="PostgresException.Detail" /> and
        /// <see cref="PostgresNotice.Detail" />. These can contain sensitive data.
        /// </summary>
        [Category("Obsolete")]
        [Description("When enabled, PostgreSQL error and notice details are included on PostgresException.Detail and PostgresNotice.Detail. These can contain sensitive data.")]
        [DisplayName("Include Error Details")]
        [OpenGaussConnectionStringProperty]
        [Obsolete("Use OpenGaussConnectionStringBuilder.IncludeErrorDetail instead")]
        public bool IncludeErrorDetails
        {
            get => IncludeErrorDetail;
            set => IncludeErrorDetail = value;
        }

        #endregion

        #region Misc

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
                throw new ArgumentException("Host can't be null");
            if (Multiplexing && !Pooling)
                throw new ArgumentException("Pooling must be on to use multiplexing");
            if (SslMode == SslMode.Require && !TrustServerCertificate)
                throw new OpenGaussException(
                    "To validate server certificates, please use VerifyFull or VerifyCA instead of Require. " +
                    "To disable validation, explicitly set 'Trust Server Certificate' to true. " +
                    "See https://www.opengauss.org/doc/release-notes/6.0.html for more details.");
            if (TrustServerCertificate && (SslMode == SslMode.Allow || SslMode == SslMode.VerifyCA || SslMode == SslMode.VerifyFull))
                throw new OpenGaussException($"TrustServerCertificate=true is not supported with SslMode={SslMode}");
        }

        internal string ToStringWithoutPassword()
        {
            var clone = Clone();
            clone.Password = null;
            return clone.ToString();
        }

        internal string ConnectionStringForMultipleHosts
        {
            get
            {
                var clone = Clone();
                clone[nameof(TargetSessionAttributes)] = null;
                return clone.ConnectionString;
            }
        }

        internal OpenGaussConnectionStringBuilder Clone() => new(ConnectionString);

        internal static bool TrySplitHostPort(ReadOnlySpan<char> originalHost, [NotNullWhen(true)] out string? host, out int port)
        {
            var portSeparator = originalHost.LastIndexOf(':');
            if (portSeparator != -1)
            {
                var otherColon = originalHost.Slice(0, portSeparator).LastIndexOf(':');
                var ipv6End = originalHost.LastIndexOf(']');
                if (otherColon == -1 || portSeparator > ipv6End && otherColon < ipv6End)
                {
                    port = originalHost.Slice(portSeparator + 1).ParseInt();
                    host = originalHost.Slice(0, portSeparator).ToString();
                    return true;
                }
            }

            port = -1;
            host = null;
            return false;
        }

        internal static bool IsUnixSocket(string host, int port, [NotNullWhen(true)] out string? socketPath, bool replaceForAbstract = true)
        {
            socketPath = null;
            if (string.IsNullOrEmpty(host))
                return false;

            var isPathRooted = Path.IsPathRooted(host);

            if (host[0] == '@')
            {
                if (replaceForAbstract)
                    host = $"\0{host.Substring(1)}";
                isPathRooted = true;
            }

            if (isPathRooted)
            {
                socketPath = Path.Combine(host, $".s.PGSQL.{port}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object? obj)
            => obj is OpenGaussConnectionStringBuilder o && EquivalentTo(o);

        /// <summary>
        /// Hash function.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Host?.GetHashCode() ?? 0;

        #endregion

        #region IDictionary<string, object>

        /// <summary>
        /// Gets an <see cref="ICollection" /> containing the keys of the <see cref="OpenGaussConnectionStringBuilder"/>.
        /// </summary>
        public new ICollection<string> Keys => base.Keys.Cast<string>().ToArray()!;

        /// <summary>
        /// Gets an <see cref="ICollection" /> containing the values in the <see cref="OpenGaussConnectionStringBuilder"/>.
        /// </summary>
        public new ICollection<object?> Values => base.Values.Cast<object?>().ToArray();

        /// <summary>
        /// Copies the elements of the <see cref="OpenGaussConnectionStringBuilder"/> to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional Array that is the destination of the elements copied from <see cref="OpenGaussConnectionStringBuilder"/>.
        /// The Array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            foreach (var kv in this)
                array[arrayIndex++] = kv;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="OpenGaussConnectionStringBuilder"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            foreach (var k in Keys)
                yield return new KeyValuePair<string, object?>(k, this[k]);
        }

        #endregion IDictionary<string, object>

        #region ICustomTypeDescriptor

        /// <inheritdoc />
        protected override void GetProperties(Hashtable propertyDescriptors)
        {
            // Tweak which properties are exposed via TypeDescriptor. This affects the VS DDEX
            // provider, for example.
            base.GetProperties(propertyDescriptors);

            var toRemove = propertyDescriptors.Values
                .Cast<PropertyDescriptor>()
                .Where(d =>
                    !d.Attributes.Cast<Attribute>().Any(a => a is OpenGaussConnectionStringPropertyAttribute) ||
                    d.Attributes.Cast<Attribute>().Any(a => a is ObsoleteAttribute)
                )
                .ToList();
            foreach (var o in toRemove)
                propertyDescriptors.Remove(o.DisplayName);
        }

        #endregion

        internal static readonly string[] EmptyStringArray = new string[0];
    }

    #region Attributes

    /// <summary>
    /// Marks on <see cref="OpenGaussConnectionStringBuilder"/> which participate in the connection
    /// string. Optionally holds a set of synonyms for the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OpenGaussConnectionStringPropertyAttribute : Attribute
    {
        /// <summary>
        /// Holds a list of synonyms for the property.
        /// </summary>
        public string[] Synonyms { get; }

        /// <summary>
        /// Creates a <see cref="OpenGaussConnectionStringPropertyAttribute"/>.
        /// </summary>
        public OpenGaussConnectionStringPropertyAttribute()
        {
            Synonyms = OpenGaussConnectionStringBuilder.EmptyStringArray;
        }

        /// <summary>
        /// Creates a <see cref="OpenGaussConnectionStringPropertyAttribute"/>.
        /// </summary>
        public OpenGaussConnectionStringPropertyAttribute(params string[] synonyms)
        {
            Synonyms = synonyms;
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// An option specified in the connection string that activates special compatibility features.
    /// </summary>
    public enum ServerCompatibilityMode
    {
        /// <summary>
        /// No special server compatibility mode is active
        /// </summary>
        None,
        /// <summary>
        /// The server is an Amazon Redshift instance.
        /// </summary>
        Redshift,
        /// <summary>
        /// The server is doesn't support full type loading from the PostgreSQL catalogs, support the basic set
        /// of types via information hardcoded inside OpenGauss.
        /// </summary>
        NoTypeLoading,
    }

    /// <summary>
    /// Specifies how to manage SSL.
    /// </summary>
    public enum SslMode
    {
        /// <summary>
        /// SSL is disabled. If the server requires SSL, the connection will fail.
        /// </summary>
        Disable,
        /// <summary>
        /// Prefer non-SSL connections if the server allows them, but allow SSL connections.
        /// </summary>
        Allow,
        /// <summary>
        /// Prefer SSL connections if the server allows them, but allow connections without SSL.
        /// </summary>
        Prefer,
        /// <summary>
        /// Fail the connection if the server doesn't support SSL.
        /// </summary>
        Require,
        /// <summary>
        /// Fail the connection if the server doesn't support SSL. Also verifies server certificate.
        /// </summary>
        VerifyCA,
        /// <summary>
        /// Fail the connection if the server doesn't support SSL. Also verifies server certificate with host's name.
        /// </summary>
        VerifyFull
    }

    /// <summary>
    /// Specifies how the mapping of arrays of
    /// <a href="https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/value-types">value types</a>
    /// behaves with respect to nullability when they are requested via an API returning an <see cref="object"/>.
    /// </summary>
    public enum ArrayNullabilityMode
    {
        /// <summary>
        /// Arrays of value types are always returned as non-nullable arrays (e.g. <c>int[]</c>).
        /// If the PostgreSQL array contains a NULL value, an exception is thrown. This is the default mode.
        /// </summary>
        Never,
        /// <summary>
        /// Arrays of value types are always returned as nullable arrays (e.g. <c>int?[]</c>).
        /// </summary>
        Always,
        /// <summary>
        /// The type of array that gets returned is determined at runtime.
        /// Arrays of value types are returned as non-nullable arrays (e.g. <c>int[]</c>)
        /// if the actual instance that gets returned doesn't contain null values
        /// and as nullable arrays (e.g. <c>int?[]</c>) if it does.
        /// </summary>
        /// <remarks>When using this setting, make sure that your code is prepared to the fact
        /// that the actual type of array instances returned from APIs like <see cref="OpenGaussDataReader.GetValue"/>
        /// may change on a row by row base.</remarks>
        PerInstance,
    }

    /// <summary>
    /// Specifies whether the connection shall be initialized as a physical or
    /// logical replication connection
    /// </summary>
    /// <remarks>
    /// This enum and its corresponding property are intentionally kept internal as they
    /// should not be set by users or even be visible in their connection strings.
    /// Replication connections are a special kind of connection that is encapsulated in
    /// <see cref="PhysicalReplicationConnection"/>
    /// and <see cref="LogicalReplicationConnection"/>.
    /// </remarks>
    enum ReplicationMode
    {
        /// <summary>
        /// Replication disabled. This is the default
        /// </summary>
        Off,
        /// <summary>
        /// Physical replication enabled
        /// </summary>
        Physical,
        /// <summary>
        /// Logical replication enabled
        /// </summary>
        Logical
    }

    /// <summary>
    /// Specifies server type preference.
    /// </summary>
    enum TargetSessionAttributes : byte
    {
        /// <summary>
        /// Any successful connection is acceptable.
        /// </summary>
        Any = 0,

        /// <summary>
        /// Session must accept read-write transactions by default (that is, the server must not be in hot standby mode and the
        /// <c>default_transaction_read_only</c> parameter must be off).
        /// </summary>
        ReadWrite = 1,

        /// <summary>
        /// Session must not accept read-write transactions by default (the converse).
        /// </summary>
        ReadOnly = 2,

        /// <summary>
        /// Server must not be in hot standby mode.
        /// </summary>
        Primary = 3,

        /// <summary>
        /// Server must be in hot standby mode.
        /// </summary>
        Standby = 4,

        /// <summary>
        /// First try to find a primary server, but if none of the listed hosts is a primary server, try again in <see cref="Any"/> mode.
        /// </summary>
        PreferPrimary = 5,

        /// <summary>
        /// First try to find a standby server, but if none of the listed hosts is a standby server, try again in <see cref="Any"/> mode.
        /// </summary>
        PreferStandby = 6,
    }

    #endregion
}
