pragma solidity ^0.4.11;
import "./erc223Token.sol";
import "./lykkeTokenErc223Base.sol";
import "./SafeMath.sol";

contract EmissiveErc223Token is LykkeTokenErc223Base {
    using SafeMath for uint;
    
    function EmissiveErc223Token(
        address issuer,
        string tokenName,
        uint8 divisibility,
        string tokenSymbol, 
        string version) LykkeTokenErc223Base(issuer, tokenName, divisibility, tokenSymbol, version, 0) public{
        balances [_issuer] = SafeMath.GET_MAX_UINT256();
    }

    function totalSupply () view public returns (uint256 supply) {
        return SafeMath.GET_MAX_UINT256().sub(balances [_issuer]);
    }

    function balanceOf (address _owner) view public returns (uint256 balance) {
        return _owner == _issuer ? 0 : ERC223Token.balanceOf (_owner);
    }
}
