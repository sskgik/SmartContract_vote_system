using System;
using Miyabi.Asset.Models;
using Miyabi.Binary.Models;
using Miyabi.Common.Models;
using Miyabi.ContractSdk;
using Miyabi.Contract.Models;
using Miyabi.ModelSdk.Execution;

namespace Miyabi.Tests.SCs
{
    public class Vote : ContractBase
    {
        static string  VoteTable = "VoteChaCha";               //投票の管理テーブル(AssetTable)
        static string  VotemanagementTable = "VoteChaChaManagement"; //参加者及びイベント内容(binaryTable)

        public Vote(ContractInitializationContext ctx) : base(ctx)
        {

        }

        /// <summary>
        /// SmartContract Instance
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public override bool Instantiate(string[] arg)
        {
            //Assettableownerkwy = contract admin key 
            var contractAdmin = new[]
            {
                GetContractAddress(),
            };

            var voteTableName = GetVoteTableName();
            //AssetDiscripter(tablename,tracked ,proof,contractadmin(tableowner))
            var assettableDescriptor = new AssetTableDescriptor(voteTableName, false,false,contractAdmin);

            var voteManagementTableName = GetVoteManagementTableName();
            //BinarytableDescriptor(tablename,tracks)
            var binarytableDescriptor = new BinaryTableDescriptor(voteManagementTableName, false);

            try
            {
                //statewrite is environment hold
                StateWriter.AddTable(assettableDescriptor); //votetable
                StateWriter.AddTable(binarytableDescriptor);//votemanagement
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ParticipateEvent method (投票イベントへの参加登録)
        /// </summary>
        /// <param name="ParticipateAddress"></param>
        public void  ParticipateEvent(Address ParticipateAddress, string Eventname)
        {
            var votemanageTableName = GetVoteManagementTableName();
            //TryGetTableWriter:(StateWriterに登録されたテーブルがあればtrue)
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(votemanageTableName, out var managementTable))   
            {
                return;
            }


            string registinfo = ParticipateAddress.ToString() +"_"+ Eventname;
            //すでに登録されているかチェック
            if (managementTable.TryGetValue(ByteString.Parse(registinfo), out var value))
            {
                return;
            }

            //Binary values ​​are set in the participant table
            managementTable.SetValue(ByteString.Encode(registinfo), ByteString.Encode("Checked"));
        }

        /// <summary>
        /// Distribute votes method(スマートコントラクトからイベント参加者のみに票を配る)
        /// </summary>
        /// <param name="RequesterAddress"></param>
        /// <param name="Familyname"></param>
        public void Distributevotes(Address RequesterAddress, string Eventname, decimal votenum )
        {
            var voteManagementtable = GetVoteManagementTableName();
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(voteManagementtable, out var votemanageTable))
            {
                return;
            }
            //イベントの参加者登録の確認
            string participantinfo = RequesterAddress.ToString() + "_" + Eventname;
            if(!votemanageTable.TryGetValue(ByteString.Encode(participantinfo),out var value))
            {
                return;
            }

            var Votetable = GetVoteTableName();
            if (!StateWriter.TryGetTableWriter<IAssetTableWriter>(Votetable, out var votetable))
            {
                return;
            }
            var contractaddress = GetContractAddress();
            votetable.MoveValue(contractaddress, RequesterAddress, votenum);
        }

        public void vote(Address voteraddress,Address votetargetAddress,string Eventname,decimal votenum)
        {
            int i;
            var voteManagementtable = GetVoteManagementTableName();
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(voteManagementtable, out var votemanageTable))
            {
                return;
            }
            //イベントの参加者登録の確認
            string participantinfo = voteraddress.ToString() + "_" + Eventname;
            if (!votemanageTable.TryGetValue(ByteString.Encode(participantinfo), out var value))
            {
                return;
            }
            //イベント情報と投票先ターゲットの紹介
            if(!votemanageTable.TryGetValue(ByteString.Encode(Eventname),out var targetnum))
            {
                return;
            }
            int targetnumber = Convert.ToInt32(targetnum.Decode());
            for(i=0;i<=targetnumber-1;i++)
            {
                votemanageTable.TryGetValue(ByteString.Encode(Eventname + Convert.ToString(i + 1)), out var address);
                var confirmaddress = PublicKeyAddress.Decode(address);
                if(votetargetAddress == confirmaddress)
                {
                    break;
                }
                else if(i == targetnumber-1)
                {
                    return;
                }
            }
            var Votetable = GetVoteTableName();
            if (!StateWriter.TryGetTableWriter<IAssetTableWriter>(Votetable, out var votetable))
            {
                return;
            }
            votetable.MoveValue(voteraddress, votetargetAddress, votenum);
        }

        /// <summary>
        ///  Confirm info of participant(参加情報の登録の可否確認)=>querymethod
        /// </summary>
        /// <param name="confirmaddress"></param>
        /// <param name="Eventname"></param>
        /// <returns></returns>
        public ByteString Confirmpaticipate(Address confirmaddress , string Eventname)
        {
            var votemanageTable = GetVoteManagementTableName();
            if (!StateWriter.TryGetTableReader<IBinaryTableReader>(votemanageTable, out var voteinfotable))
            {
                return null;
            }
            string requestinfokey = confirmaddress.ToString() + "_" + Eventname;
            voteinfotable.TryGetValue(ByteString.Encode(requestinfokey), out var value);
            return value;
        }

        /// <summary>
        /// RegistIssuanceRight method(票の発行権を持つアドレスを登録)
        /// </summary>
        /// <param name="InheritanceAddress"></param>
        public void RegistIssuanceRight(Address IssuanceRightAddress)
        {

            SetInternalValue( ByteString.Encode ("Issuance_Right"), IssuanceRightAddress.Encoded);
        }

        /// <summary>
        /// registvotetarget(投票イベントの登録)
        /// </summary>
        /// <param name="votetargetAddress"></param>
        /// <param name="Eventname"></param>
        /// <param name="votetargetnum"></param>
        public void registvotetarget(Address[] votetargetAddress,string Eventname ,int votetargetnum)
        {
            int i;
            var votemanagetable = GetVoteManagementTableName();
            if (!StateWriter.TryGetTableWriter<IBinaryTableWriter>(votemanagetable, out var managetable))
            {
                return;
            }
            //イベントデータをKeyとした時の情報の有無を確認あればデータ登録はしない
            if (managetable.TryGetValue(ByteString.Encode(Eventname),out var value))
            {
                return;
            }
            string num = Convert.ToString(votetargetnum);
            managetable.SetValue(ByteString.Encode(Eventname), ByteString.Encode(num));
            for(i=0;i<=votetargetnum-1;i++)
            {
                managetable.SetValue(ByteString.Encode(Eventname + Convert.ToString(i + 1)), votetargetAddress[i].Encoded);
            }
        }

        /// <summary>
        /// ReplenishmentVotes method(予め登録されたアドレスのみ票の発行を受け付ける)
        /// </summary>
        /// <param name="TestatorAddress"></param>
        /// <param name="amount"></param>
        public void ReplenishmentVotes(Address RequestAddress,decimal amount)
        {
            if(!TryGetInternalValue(ByteString.Encode("Issuance_Right"), out var address))
            {
                return;
            }
            var checkaddress = PublicKeyAddress.Decode(address); 
            if(RequestAddress != checkaddress)
            {
                return;
            }
            var voteTableName = GetVoteTableName();
            if (!StateWriter.TryGetTableWriter<IAssetTableWriter>(voteTableName, out var table))
            {
                return;
            }
            var contractAddress = GetContractAddress();
            table.MoveValue(table.VoidAddress, contractAddress, amount);
        }

        public bool TryGetInternalValue(ByteString key, out ByteString value)
        {
            return InternalTable.TryGetValue(key, out value);
        }

        public void SetInternalValue(ByteString key,ByteString value)
        {
            if(InternalTable is IPermissionedBinaryTableWriter internalTableWriter)
            {
                internalTableWriter.CreateOrUpdateValue(key, value);
            }
            else
            {
                throw new InvalidOperationException("Context is readOnly");
            }
        }

        //金融資産の管理テーブル(AssetTable)
        private string GetVoteTableName()
        {
            return AddInstanceSuffix(VoteTable);
        }

        //相続情報及び遺言者の生存管理テーブル(binaryTable)
        private string GetVoteManagementTableName()
        {
            return AddInstanceSuffix(VotemanagementTable);
        }

        private string AddInstanceSuffix(string tableName)
        {
            return tableName + InstanceName;
        }

        private Address GetContractAddress()
        {
            return ContractAddress.FromInstanceId(InstanceId);
        }
    }
}
