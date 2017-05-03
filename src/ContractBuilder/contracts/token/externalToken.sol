pragma solidity ^0.4.9;
import "./erc20Token.sol";

contract ExternalToken is ERC20Token {

  address private _issuer;

  function ExternalToken(address issuer) {
    _issuer = issuer;
    accounts [_issuer] = MAX_UINT256;
  }

  function totalSupply () constant returns (uint256 supply) {
    return safeSub (MAX_UINT256, accounts [_issuer]);
  }

  function balanceOf (address _owner) constant returns (uint256 balance) {
    return _owner == _issuer ? 0 : ERC20Token.balanceOf (_owner);
  }
}
