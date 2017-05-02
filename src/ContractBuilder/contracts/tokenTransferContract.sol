pragma solidity ^0.4.9;
import "./coin.sol";
import "./transferBaseContract.sol";
import "./erc20Contract.sol";

contract TokenTransferContract is TransferBaseContract{

    address _externalTokenAddress;

    modifier onlyowner { if (msg.sender == _owner) _; }

    function TokenTransferContract(address userAddress, address coinAdapterAddress, address externalTokenAddress) 
        TransferBaseContract(userAddress, coinAdapterAddress) {
            _externalTokenAddress = externalTokenAddress;
    }

    function cashin() onlyowner {
        var erc20Token = ERC20Interface(_externalTokenAddress);
        var tokenBalance = erc20Token.balanceOf(this);
        if (tokenBalance <= 0) {
            throw;
        }

        var coin_contract = Coin(_coinAdapterAddress);
        coin_contract.cashin(_userAddress, tokenBalance);
    }
}
