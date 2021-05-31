using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TONBRAINS.TELEGRAMOPENNETWORK.APP.Models;
using TONBRAINS.TONOPS.Core.DAL;
using TONBRAINS.TONOPS.Core.Extensions;
using TONBRAINS.TONOPS.Core.Handlers;
using TONBRAINS.TONOPS.Core.Helpers;
using TONBRAINS.TONOPS.Core.Models;
using TONBRAINS.TONOPS.Core.SSH;
using TONBRAINS.TONOPS.Ffi;

namespace TONBRAINS.TONOPS.Core.Services
{
    public class TONNodeSetupSvc
    {


        public bool StartTonNetwork(params Node[] nodes)
        {
            StopValidatorEngine(nodes);
            PreInit_Ext(nodes);
            SetupZeroState(nodes);
            RunValidatorEngine(nodes);


            return true;
        }


        public Dictionary<string, bool> InstallDependcy(params Node[] nodes)
        {
            return new SSHCommandHlp(nodes).ExecuteCommandsParallelBash("ton_install_dependency");
        }

        public Dictionary<string, bool> CloneAndBuildTonRepo(params Node[] nodes)
        {
            var content = new CommonHelprs().GetBashContentFromFile("ton_clone_repo").ReplaceByConfigValues();
            return new SSHCommandHlp(nodes).ExecuteCommandsParallelBashByContent(content);
        }


        public bool PreInit_Ext(params Node[] nodes)
        {
            var CommonHelprs = new CommonHelprs();
            var fakeConfig = CommonHelprs.GetFiletoByteForTonConfig("ton-globalfake.config.json");
            var tasks = new List<Task>();
            foreach (var n in nodes)
            {
                var task = Task.Run(() =>
                {
                    var NODE_ADDRESS = $"{n.Ip}:{GlobalVarHandler.ADNL_PORT}";
                    var SSHCommandHlp = new SSHCommandHlp(n);

                    //reabuild ton_work directory
                    RebuildTonWorkDir(n);
                    var NODE_PREINIT_CONFIGS_DIR = $"{GlobalVarHandler.CONFIGS_DIR}/preinit_{n.Ip}";
                    SSHCommandHlp.DeleteCreateDirectory(GlobalVarHandler.CONFIGS_DIR);
                    SSHCommandHlp.CreateDirectory(NODE_PREINIT_CONFIGS_DIR);

                    //Upload Fake Ton config
                    SSHCommandHlp.UploadFileToHost(fakeConfig, GlobalVarHandler.TON_GLOBAL_FAKECONFIG);

                    //init validator Ton fake db config
                    ExcuteValidatorEngineIComands($"--global-config {GlobalVarHandler.TON_GLOBAL_FAKECONFIG.ToQQ()} --db {GlobalVarHandler.TON_WORK_DB.ToQQ()} --ip {NODE_ADDRESS}", n);
                    //// rebuild /root/ton-keys dir
                    RebuildTonKeysDir(n);

                    ////generate keys
                    var serverKey = ExcuteGenerateRadomIdComandsWithResult("-m keys -n server", n).First().Value.First().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString($"{serverKey[0]} {serverKey[1]}"), GlobalVarHandler.KEYS_DIR_KEYS_S);
                    var serverKeyringPath = $"{GlobalVarHandler.TON_WORK_DB_KEYRING}/{serverKey[0]}";
                    SSHCommandHlp.ExecuteCommands(
                        $"mv {GlobalVarHandler.ROOT_DIR}/server {GlobalVarHandler.TON_WORK_DB_KEYRING}/{serverKey[0]}",
                        $"mv {GlobalVarHandler.ROOT_DIR}/server.pub {GlobalVarHandler.KEYS_DIR}/server.pub"
                        );


                    var validatorKey = ExcuteGenerateRadomIdComandsWithResult("-m keys -n validator", n).First().Value.First().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    var validatorPath = $"{GlobalVarHandler.KEYS_DIR}/keys_v";
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString($"{validatorKey[0]} {validatorKey[1]}"), validatorPath);
                    SSHCommandHlp.ExecuteCommands(
                        $"mv {GlobalVarHandler.ROOT_DIR}/validator {GlobalVarHandler.TON_WORK_DB_KEYRING}/{validatorKey[0]}",
                        $"mv {GlobalVarHandler.ROOT_DIR}/validator.pub {GlobalVarHandler.KEYS_DIR}/validator.pub"
                        );

                    var validatorubPath = $"{GlobalVarHandler.KEYS_DIR}/validator.pub";
                    var keypubPath = $"{NODE_PREINIT_CONFIGS_DIR}/{n.Ip}-key.pub";
                    SSHCommandHlp.ExecuteCommands($"dd skip=4 count=32 if={validatorubPath.ToQQ()} of={keypubPath.ToQQ()} bs=1 status=none");

                    var adnlKey = ExcuteGenerateRadomIdComandsWithResult("-m keys -n adnl", n).First().Value.First().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    var adnlKeyPath = $"{GlobalVarHandler.KEYS_DIR}/keys_a";
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString($"{adnlKey[0]} {adnlKey[1]}"), adnlKeyPath);
                    SSHCommandHlp.ExecuteCommands(
            $"mv {GlobalVarHandler.ROOT_DIR}/adnl {GlobalVarHandler.TON_WORK_DB_KEYRING}/{adnlKey[0]}",
            $"mv {GlobalVarHandler.ROOT_DIR}/adnl.pub {GlobalVarHandler.KEYS_DIR}/adnl.pub"
            );

                    var liteserverKey = ExcuteGenerateRadomIdComandsWithResult("-m keys -n liteserver", n).First().Value.First().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    var liteserverKeyPath = $"{GlobalVarHandler.KEYS_DIR}/keys_l";
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString($"{liteserverKey[0]} {liteserverKey[1]}"), liteserverKeyPath);
                    SSHCommandHlp.ExecuteCommands(
$"mv {GlobalVarHandler.ROOT_DIR}/liteserver {GlobalVarHandler.TON_WORK_DB_KEYRING}/{liteserverKey[0]}",
$"mv {GlobalVarHandler.ROOT_DIR}/liteserver.pub {GlobalVarHandler.KEYS_DIR}/liteserver.pub"
);
                    var clientKey = ExcuteGenerateRadomIdComandsWithResult("-m keys -n client", n).First().Value.First().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    var clientPath = $"{GlobalVarHandler.KEYS_DIR}/keys_c";
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString($"{clientKey[0]} {clientKey[1]}"), clientPath);
                    SSHCommandHlp.ExecuteCommands(
$"mv {GlobalVarHandler.ROOT_DIR}/client.pub {GlobalVarHandler.KEYS_DIR}/client.pub",
$"mv {GlobalVarHandler.ROOT_DIR}/client {GlobalVarHandler.KEYS_DIR}/client"
);


                 
                    var initConfigBytea = SSHCommandHlp.DownloadFileToByteaHost(GlobalVarHandler.TON_WORK_DB_CONFIG);
                    string initConfigString = Encoding.Default.GetString(initConfigBytea.First().Value);
                    var initConfig = JsonConvert.DeserializeObject<InitTonDbConfigMdl>(initConfigString);

                    var udp_Ip = initConfig.addrs.First().ip;
                    var engine_adnl_key = initConfig.adnl.First(q => q.category == 1).id;
                    var engine_dht_key = initConfig.adnl.First(q => q.category == 0).id;
                    var dateTimeNow_unix = UnixTimeHelper.UnixTimeUTCNow()  + 100000;
                    //setup full Ton Db Config
                    var fullConfig = new CommonHelprs().GetFiletoStringForTonConfig("ton-db.config.json");
                    var fullConfigString = fullConfig.Replace("${validator_id}", validatorKey[1])
                        .Replace("${validator_id}", validatorKey[1])
                        .Replace("${adnl_id}", adnlKey[1])
                        .Replace("${adnl_id}", adnlKey[1])
                        .Replace("${server_id}", serverKey[1])
                        .Replace("${liteserver_id}", liteserverKey[1])
                        .Replace("$udp_Ip", udp_Ip.ToString())
                        .Replace("${client_id}", clientKey[1])
                        .Replace("${engine_adnl}", engine_adnl_key)
                        .Replace("${engine_dht}", engine_dht_key).Replace("$expire_at", dateTimeNow_unix.ToString());

                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString(fullConfigString), GlobalVarHandler.TON_WORK_DB_CONFIG);
                    var engine_dht_key_hex = engine_dht_key.FromBase64ToTonHex();

                    var dhtPath = $"{NODE_PREINIT_CONFIGS_DIR}/{n.Ip}-dht";
                    var c1 = $"-k {GlobalVarHandler.TON_WORK_DB_KEYRING}/{engine_dht_key_hex}";
                    var c0 = "{\\\"@type\\\":\\\"adnl.addressList\\\",\\\"addrs\\\":[{\\\"@type\\\":\\\"adnl.address.udp\\\",\\\"ip\\\" : " + udp_Ip + ",\\\"port\\\":30310 }]}";
                    var dhtSignature = ExcuteGenerateRadomIdComandsWithResult($"-m dht -a {c0.ToQQ()} {c1}", n).First().Value.First();
                    SSHCommandHlp.UploadFileToHost(CommonHelprs.GenerateByteaFromString(dhtSignature), dhtPath);

                    SSHCommandHlp.DeleteFile(GlobalVarHandler.TON_GLOBAL_FAKECONFIG);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            return true;
        }


        public Dictionary<string, bool> ExcuteValidatorEngineIComands(string cmd, params Node[] nodes)
        {
            var validatorBuildPath = GlobalVarHandler.TON_BUILD_DIR + "/validator-engine/validator-engine";
            return new SSHCommandHlp(nodes).ExecuteCommandsParallel($"{validatorBuildPath.ToQQ()} {cmd}");
        }


        public Dictionary<string, IEnumerable<string>> ExcuteGenerateRadomIdComandsWithResult(string cmd, params Node[] nodes)
        {
            var validatorBuildPath = GlobalVarHandler.UTILS_DIR + "/generate-random-id";
            return new SSHCommandHlp(nodes).ExecuteCommandsWithResultParallel($"{validatorBuildPath.ToQQ()} {cmd}");
        }


   
        public Dictionary<string, bool> CreateTonWorkDbStaticDir(IEnumerable<Node> nodes)
        {
            var cmds = new List<string>
            {
                $"rm -rf {GlobalVarHandler.TON_WORK_STATIC_DIR.ToQQ()}",
                $"mkdir -p {GlobalVarHandler.TON_WORK_STATIC_DIR.ToQQ()}",
            };

            return new SSHCommandHlp(nodes).ExecuteCommandsParallel(cmds);
        }

        public Dictionary<string, bool> RebuildTonKeysDir(params Node[] nodes)
        {
            var cmds = new List<string>
            {
                $"rm -rf {GlobalVarHandler.KEYS_DIR.ToQQ()}",
                $"mkdir -p {GlobalVarHandler.KEYS_DIR.ToQQ()}",
                $"mkdir  chown {"0:0".ToQQ()} {GlobalVarHandler.KEYS_DIR.ToQQ()}", // assign owner
                $"chmod 700 {GlobalVarHandler.KEYS_DIR.ToQQ()}",
            };
            return new SSHCommandHlp(nodes).ExecuteCommandsParallel(cmds);
        }

        public Dictionary<string, bool> RebuildTonWorkDir(params Node[] nodes)
        {
            var db = $"{GlobalVarHandler.TON_WORK_DIR}/db";
            var etc = $"{GlobalVarHandler.TON_WORK_DIR}/etc";
            var cmds = new List<string>
            {
                $"rm -rf {GlobalVarHandler.TON_WORK_DIR.ToQQ()}",
                $"mkdir -p {GlobalVarHandler.TON_WORK_DIR.ToQQ()}",
                $"mkdir  chown {"0:0".ToQQ()} {GlobalVarHandler.TON_WORK_DIR.ToQQ()}", // assign owner
                $"mkdir -p {db.ToQQ()}",
                $"mkdir -p {etc.ToQQ()}",
            };

            return new SSHCommandHlp(nodes).ExecuteCommandsParallel(cmds);
        }

        public Dictionary<string, string> GetPreInitDht(IEnumerable<Node> nodes)
        {

            var rs = DictionaryExt.GetConcurrentDefaulStringDic();
            var tasks = nodes.Select(q => Task.Run(() => {

                var cmd = $"cat {GlobalVarHandler.CONFIGS_DIR}/preinit_{q.Ip}/{q.Ip}-dht";
                var d = new SSHCommandHlp(q).ExecuteCommandsWithResult(cmd).First();
                rs.TryAdd(q.Id, d.Value.First()); 
            }));

            Task.WaitAll(tasks.ToArray());

            return rs.ConvertConcurrentToDic();
        }


        public bool StopValidatorEngine(params Node[] nodes)
        {
            var r = new SSHCommandHlp(nodes).ExecuteCommandsAsBashParallel(new string[] { "pkill -f validator-engine" });
            return true;
        }

        public bool RunValidatorEngine(params Node[] nodes)
        {
            var cmd = $"{GlobalVarHandler.VALIDATOR_ENGINE.ToQQ()} -C {GlobalVarHandler.TON_WORK_GLOBAL_CONFIG_PATH.ToQQ()} --db {GlobalVarHandler.TON_WORK_DB.ToQQ()} --logname {GlobalVarHandler.TON_WORK_PROCNODE_LOG.ToQQ()} --daemonize | head -c 1G > {GlobalVarHandler.TON_WORK_NODE_LOG.ToQQ()} 2>&1 &";
            var r = new SSHCommandHlp(nodes).ExecuteCommandsAsBashParallel(new string[] { "pkill -f validator-engine", cmd });
            return true;
        }



        public string GetTonGlobalConfig(IEnumerable<Node> nodes, string zerostate0_roothash, string zerostate0_filehash)
        {

            var confParts = GetPreInitDht(nodes).Select(q => q.Value);

            var ton_global_config = new CommonHelprs().GetFiletoStringForTonConfig("ton-global.config.json");
            ton_global_config = ton_global_config.Replace("\"${nodes}\"", string.Join(",", confParts)).Replace("${root_hash}", zerostate0_roothash).Replace("${file_hash}", zerostate0_filehash);

            return ton_global_config;
        }

        public byte[] GetValidatorPubKey(IEnumerable<Node> nodes)
        {
            var rs = DictionaryExt.GetConcurrentDefaultByteaDic();

            var tasks = nodes.Select(q => Task.Run(() => {

                var p = $"{GlobalVarHandler.TON_SRC_DIR}/configs/preinit_{q.Ip}/{q.Ip}-key.pub";
                var d = new SSHCommandHlp(q).DownloadFileToByteaHost(p).First();
                rs.TryAdd(q.Id, d.Value);
            }));

            Task.WaitAll(tasks.ToArray());
            return new CommonHelprs().CombineArray(rs.ConvertConcurrentToDic().Select(q=>q.Value).ToList());
        }

        public byte[] ConfigureZeroState(string mainConfigName, int nodeCount)
        {
            var config = new TonConfiguration();
            var CommonHelprs = new CommonHelprs();
            var genzerostateConfig = CommonHelprs.GetFiletoStringForFift("gen-zerostate-01")
                .Replace("${GlobalId}", config.GlobalId.ToString())
                .Replace("${DefaultSubwalletId}", config.DefaultSubwalletId)
                .Replace("${StartAt}", config.StartAt)
                .Replace("${ActualMinSplit}", config.ActualMinSplit.ToString())
                .Replace("${MinSplitDepth}", config.MinSplitDepth.ToString())
                .Replace("${MaxSplitDepth}", config.MaxSplitDepth.ToString())
                .Replace("${WorkChainId}", config.WorkChainId.ToString())
                .Replace("${RwalletIinitPubkey}", config.RwalletIinitPubkey)
                .Replace("${StA}", config.StA)
                .Replace("${StB}", config.StB)
                .Replace("${AdvancedWalletAllocatedBalance}", config.AdvancedWalletAllocatedBalance.ToString())
                .Replace("${AdvancedWalletSplitDepth}", config.AdvancedWalletSplitDepth.ToString())
                .Replace("${AdvancedWalletTickTock}", config.AdvancedWalletTickTock.ToString())
                .Replace("${AdvancedWalletAddress}", config.AdvancedWalletAddress)
                .Replace("${AdvancedWalletCreateSetaddr}", config.AdvancedWalletCreateSetaddr.ToString())
                .Replace("${ElectorAllocatedBalance}", config.ElectorAllocatedBalance.ToString())
                .Replace("${ElectorSplitDepth}", config.ElectorSplitDepth.ToString())
                .Replace("${ElectorTickTock}", config.ElectorTickTock.ToString())
                .Replace("${ElectorAddress}", config.ElectorAddress)
                .Replace("${ElectorCreateSetaddr}", config.ElectorCreateSetaddr.ToString())
                .Replace("${MaxValidators}", config.MaxValidators.ToString())
                .Replace("${MaxMainValidators}", config.MaxMainValidators.ToString())
                .Replace("${MinValidators}", config.MinValidators.ToString())
                .Replace("${MinStake}", config.MinStake.ToString())
                .Replace("${MaxStake}", config.MaxStake.ToString())
                .Replace("${MinTotalStake}", config.MinTotalStake.ToString())
                .Replace("${MaxFactor}", config.MaxFactor.ToString())
                .Replace("${ElectedFor}", config.ElectedFor.ToString())
                .Replace("${ElectStartBefore}", config.ElectStartBefore.ToString())
                .Replace("${ElectEndBefore}", config.ElectEndBefore.ToString())
                .Replace("${StakesFrozenFor}", config.StakesFrozenFor.ToString())
                .Replace("${ElectorConfigAddress}", config.ElectorConfigAddress)
                .Replace("${MainWalletTvc_Name}", mainConfigName);
            return CommonHelprs.GenerateByteaFromString(genzerostateConfig);
        }


        public bool SetupZeroState(params Node[] nodes)
        {
            try
            {
          
                var CommonHelprs = new CommonHelprs();
  
                var SSHCommandHlpFirst = new SSHCommandHlp(nodes.First());
               



                var manWalletContractName = $"MainWallet";
                var mainWalletContractPath = $"{GlobalVarHandler.SMARTCONT_DIR}/{manWalletContractName}.tvc";




                //mnemonic: simple spray chat mosquito learn thing subject obtain pass rich observe can
                //public: 526ebfb5addc9930aaf76b8d72bcb8a4ef5aa17750236403ef01b4ebd6657281
                //secret: 8529b89da63d0249621368c35bd78e140d19619da1bde288b4374bb7f27008c6

                var mainwalletFinalTvc = CommonHelprs.GetFiletoByteForSmartContract("MainWalletWithKey.tvc");


                SSHCommandHlpFirst.ExecuteCommandsParallel($"rm -rf {mainWalletContractPath}");
                SSHCommandHlpFirst.UploadFileToHostParallel(mainwalletFinalTvc, $"{mainWalletContractPath}");




                SSHCommandHlpFirst.ExecuteCommandsParallel($"rm -rf {GlobalVarHandler.SMARTCONT_DIR_VALIDATOR_KEYS_PUB}", $"rm -rf {GlobalVarHandler.SMARTCONT_DIR_GEN_ZEROSTATE_FIF}");
                SSHCommandHlpFirst.UploadFileToHostParallel(GetValidatorPubKey(nodes), $"{GlobalVarHandler.SMARTCONT_DIR_VALIDATOR_KEYS_PUB}");
                SSHCommandHlpFirst.UploadFileToHostParallel(ConfigureZeroState(manWalletContractName, nodes.Count()), $"{GlobalVarHandler.SMARTCONT_DIR_GEN_ZEROSTATE_FIF}");


                //var configContractPath = $"{GlobalVarHandler.SMARTCONT_DIR}/auto";
                var configContractPathSMC = $"{GlobalVarHandler.SMARTCONT_DIR}/auto/config-code.fif";
                var configcodeConfigFif = CommonHelprs.GetFiletoStringForFift("config-code");
                SSHCommandHlpFirst.UploadFileToHostParallel(CommonHelprs.GenerateByteaFromString(configcodeConfigFif), $"{configContractPathSMC}");


                var valodatorsKeys = new List<string>();
                var validators_msig = new List<string>();
                var validators_addrs = new List<string>();



                var tasks = new List<Task>();
                foreach (var n in nodes)
                {

                    var task = Task.Run(() =>
                    {


                    var SSHCommandHlp = new SSHCommandHlp(n);

                    
                    SSHCommandHlp.DeleteCreateDirectory(GlobalVarHandler.TON_WORK_DB_IMPORT);



                    });

                    tasks.Add(task);
             

                }

                Task.WaitAll(tasks.ToArray());



                var zerostate_filehash_data = new string[] { };
                var basestate0_filehash_data = new string[] { };
                var zerostate_roothash_data = new string[] { };

                Gneratezerostate(SSHCommandHlpFirst, nodes.First(), out zerostate_filehash_data, out basestate0_filehash_data, out zerostate_roothash_data, out string configPrivateKey, out string configPublicKey);




                var tonGlobalConfig = GetTonGlobalConfig(nodes, zerostate_roothash_data[5].FixBase64(), zerostate_filehash_data[5].FixBase64());

                SSHCommandHlpFirst.ExecuteCommandsParallel($"rm -rf {GlobalVarHandler.TON_WORK_GLOBAL_CONFIG_PATH}");
                new SSHCommandHlp(nodes).UploadFileToHostParallel(CommonHelprs.GenerateByteaFromString(tonGlobalConfig), GlobalVarHandler.TON_WORK_GLOBAL_CONFIG_PATH);

                var zerostate_hash_file = SSHCommandHlpFirst.DownloadFileToByteaHostParallel($"{GlobalVarHandler.SMARTCONT_DIR}/zerostate.boc").First().Value;
                var base0_hash_file = SSHCommandHlpFirst.DownloadFileToByteaHostParallel($"{GlobalVarHandler.SMARTCONT_DIR}/basestate0.boc").First().Value;




                //upload global config to host
                CreateTonWorkDbStaticDir(nodes);
                new SSHCommandHlp(nodes).UploadFileToHostParallel(zerostate_hash_file, $"{GlobalVarHandler.TON_WORK_STATIC_DIR}/{zerostate_filehash_data[3]}");
                new SSHCommandHlp(nodes).UploadFileToHostParallel(base0_hash_file, $"{GlobalVarHandler.TON_WORK_STATIC_DIR}/{basestate0_filehash_data[3]}");



                return true;
            }
            catch (Exception ex)
            { 
            
            }

            return true;

        }



        public void Gneratezerostate(SSHCommandHlp SSHCommandHlpFirst, Node n, out string[] zerostate_filehash_data, out string[] basestate0_filehash_data, out string[] zerostate_roothash_data, out string configPrivateKey, out string configPublicKey)
        {

            var dumplogPath = $"{GlobalVarHandler.SMARTCONT_DIR}/dump.log";
            var s = $"{GlobalVarHandler.SMARTCONT_DIR}/gen-zerostate.fif";
            new FiftSvc().ExecuteCreateZeroState($"{s.ToQQ()} > {dumplogPath}", new Node[] { n });

            var initConfigBytea = SSHCommandHlpFirst.DownloadFileToByteaHostParallel(dumplogPath);
            string dump_log = Encoding.Default.GetString(initConfigBytea.First().Value);
            var dump_log_split = dump_log.Split("\n");

            zerostate_filehash_data = dump_log_split.Where(q => q.Contains("Zerostate file hash=")).First().Split(" ");
            basestate0_filehash_data = dump_log_split.Where(q => q.Contains("Basestate0 file hash=")).First().Split(" ");
            zerostate_roothash_data = dump_log_split.Where(q => q.Contains("Zerostate root hash=")).First().Split(" ");
            configPublicKey = dump_log_split.Where(q => q.Contains("config public key =")).First().Split(" ")[4].ToLower();
            configPrivateKey = dump_log_split.Where(q => q.Contains("config private key =")).First().Split(" ")[4].ToLower();
        }


    }
}
