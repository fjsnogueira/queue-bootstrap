CREATE SCHEMA `queue` DEFAULT CHARACTER SET utf8 ;

CREATE TABLE `queue`.`Order` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `IdStatus` INT NOT NULL,
  `Value` DECIMAL(8,2) NOT NULL,
  PRIMARY KEY (`Id`));

CREATE TABLE `queue`.`OrderStatus` (
  `Id` INT NOT NULL,
  `Description` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `Description_UNIQUE` (`Description` ASC) VISIBLE);

ALTER TABLE `queue`.`Order` 
ADD INDEX `IdStatus_idx` (`IdStatus` ASC) VISIBLE;
;
ALTER TABLE `queue`.`Order` 
ADD CONSTRAINT `IdStatus`
  FOREIGN KEY (`IdStatus`)
  REFERENCES `queue`.`OrderStatus` (`Id`)
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

INSERT INTO `queue`.`OrderStatus` (`Id`, `Description`) VALUES 
  ('1', 'Ordered'),
  ('2', 'Processed'),
  ('3', 'Paid'),
  ('4', 'Sent'),
  ('5', 'Received');

INSERT INTO `queue`.`Order` (`IdStatus`, `Value`) VALUES 
  ('1', '10.2'),
  ('1', '15.2'),
  ('4', '47.8');