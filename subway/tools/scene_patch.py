"""Small source-level readability fixes applied before SCUMM compilation."""
def apply(common,rooms):
 speech='egoPrintAt(160,3);egoPrintCenter();egoPrintClipped(310);'
 return common.replace('egoPrintOverhead();',speech),rooms.replace('egoPrintOverhead();',speech)
