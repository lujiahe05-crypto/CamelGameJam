import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';

const scriptDir = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDir, '..');
const excelDir = join(projectRoot, 'Assets', 'Excels');
const jsonDir = join(projectRoot, 'Assets', 'Resources', 'ThronefallConfigs');
const outputPath = join(excelDir, 'ThronefallConfigTemplate.xlsx');
const metaPath = `${outputPath}.meta`;

const XML_HEADER = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>';

function loadJson(fileName) {
  return JSON.parse(readFileSync(join(jsonDir, fileName), 'utf8'));
}

function escapeXml(value) {
  return String(value)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

function columnName(index) {
  let n = index + 1;
  let name = '';
  while (n > 0) {
    const remainder = (n - 1) % 26;
    name = String.fromCharCode(65 + remainder) + name;
    n = Math.floor((n - 1) / 26);
  }
  return name;
}

function makeRow(headers, values = {}) {
  return Object.fromEntries(headers.map((header) => [header, values[header] ?? '']));
}

function makeCell(ref, value) {
  if (value === '' || value === null || value === undefined) {
    return '';
  }

  if (typeof value === 'number') {
    return `<c r="${ref}"><v>${value}</v></c>`;
  }

  return `<c r="${ref}" t="inlineStr"><is><t>${escapeXml(value)}</t></is></c>`;
}

function buildSheetXml(headers, notes, rows) {
  const noteRow = makeRow(headers, Object.fromEntries(headers.map((header, index) => [header, notes[index] ?? ''])));
  const headerRow = makeRow(headers, Object.fromEntries(headers.map((header) => [header, header])));
  const allRows = [noteRow, headerRow, ...rows];

  const rowXml = allRows
    .map((row, rowIndex) => {
      const cells = headers
        .map((header, columnIndex) => {
          const ref = `${columnName(columnIndex)}${rowIndex + 1}`;
          return makeCell(ref, row[header]);
        })
        .filter(Boolean)
        .join('');
      return `<row r="${rowIndex + 1}">${cells}</row>`;
    })
    .join('');

  return `${XML_HEADER}<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>${rowXml}</sheetData></worksheet>`;
}

function buildContentTypes(sheetCount) {
  const sheetOverrides = Array.from({ length: sheetCount }, (_, index) => {
    return `<Override PartName="/xl/worksheets/sheet${index + 1}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>`;
  }).join('');

  return `${XML_HEADER}<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>${sheetOverrides}</Types>`;
}

function buildRootRels() {
  return `${XML_HEADER}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>`;
}

function buildWorkbookXml(sheetDefs) {
  const sheets = sheetDefs
    .map((sheet, index) => {
      return `<sheet name="${escapeXml(sheet.name)}" sheetId="${index + 1}" r:id="rId${index + 1}"/>`;
    })
    .join('');

  return `${XML_HEADER}<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>${sheets}</sheets></workbook>`;
}

function buildWorkbookRels(sheetCount) {
  const rels = Array.from({ length: sheetCount }, (_, index) => {
    return `<Relationship Id="rId${index + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet${index + 1}.xml"/>`;
  }).join('');

  return `${XML_HEADER}<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${rels}</Relationships>`;
}

function writeTextFile(path, content) {
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, content, 'utf8');
}

function psQuote(value) {
  return `'${String(value).replace(/'/g, "''")}'`;
}

const buildingTable = loadJson('BuildingTable.json');
const monsterTable = loadJson('MonsterTable.json');
const waveTable = loadJson('WaveTable.json');
const heroTable = loadJson('HeroTable.json');
const allyUnitTable = loadJson('AllyUnitTable.json');

const buildingHeaders = [
  'buildingId',
  'buildingName',
  'description',
  'buildingType',
  'coinCost',
  'maxHP',
  'atk',
  'def',
  'attackRange',
  'attackInterval',
  'arrowSpeed',
  'arcHeight',
  'aoeRadius',
  'dailyYield',
  'recruitCost',
  'maxRecruits',
  'upgradeIds',
  'branchIcon',
  'allyUnitType',
];

const monsterHeaders = [
  'monsterId',
  'monsterName',
  'maxHP',
  'atk',
  'def',
  'moveSpeed',
  'attackRange',
  'attackInterval',
  'iconLabel',
];

const waveHeaders = [
  'rowType',
  'key',
  'value',
  'dayNumber',
  'spawnX',
  'spawnY',
  'spawnZ',
  'monsterId',
  'count',
  'spawnInterval',
];

const heroHeaders = [
  'rowType',
  'key',
  'value',
  'weaponId',
  'weaponName',
  'atk',
  'def',
  'attackRange',
  'attackInterval',
  'arrowSpeed',
  'arcHeight',
  'skillId',
  'cooldown',
  'damageMultiplier',
  'accelTime',
  'decelTime',
  'maxSpeed',
  'arrowCount',
  'spreadAngle',
];

const allyHeaders = [
  'unitType',
  'unitName',
  'maxHP',
  'atk',
  'def',
  'moveSpeed',
  'attackRange',
  'attackInterval',
  'respawnTime',
  'arrowSpeed',
  'arcHeight',
  'kiteDistance',
  'chargeSpeed',
  'chargeDuration',
  'chargeMultiplier',
  'chargeCooldown',
];

const buildingNotes = [
  '建筑唯一 ID',
  '建筑名称',
  '功能描述',
  '类型：economic / tower / wall / barracks / base',
  '建造或升级消耗金币',
  '最大生命值',
  '攻击力，非攻击建筑填 0',
  '防御力',
  '攻击距离，非攻击建筑填 0',
  '攻击间隔，非攻击建筑填 0',
  '箭矢飞行速度，塔类填值，其他填 0',
  '抛物线最高点高度，塔类填值，其他填 0',
  '范围伤害半径，0 表示单体',
  '每日产出金币，经济类填值，其他填 0',
  '单次招募花费金币，兵营填值，其他填 0',
  '最大招募人数，兵营填值，其他填 0',
  '升级建筑 ID 列表，逗号分隔',
  '分支选择面板的图标文字',
  '兵营兵种类型，非兵营留空',
];

const monsterNotes = [
  '怪物 ID',
  '怪物名称',
  '最大生命值',
  '攻击力',
  '防御力',
  '移动速度',
  '攻击距离',
  '攻击间隔（秒）',
  '波次预警图标字符',
];

const waveNotes = [
  '行类型：setting 或 spawn',
  '配置键名，setting 行必填',
  '配置值，setting 行必填',
  '第几天，从 1 开始，spawn 行必填',
  '刷怪点 X 坐标',
  '刷怪点 Y 坐标，通常为 0',
  '刷怪点 Z 坐标',
  '刷出的怪物 ID',
  '该类怪物数量',
  '两只怪之间的间隔（秒）',
];

const heroNotes = [
  '行类型：hero / weapon / skill',
  '配置键名，hero 行使用',
  '配置值，hero 行使用',
  '武器唯一标识，weapon 和 skill 行使用',
  '武器显示名称，weapon 行使用',
  '基础攻击力，weapon 行使用',
  '附加防御力，weapon 行使用',
  '攻击距离，weapon 行使用',
  '攻击间隔（秒），weapon 行使用',
  '武器远程飞行速度 / 技能齐射飞行速度',
  '武器抛物线高度 / 技能齐射抛物线高度',
  '技能唯一标识，skill 行使用',
  '技能冷却时间（秒）',
  '技能伤害倍率',
  '突刺加速时长，弓箭填 0',
  '突刺减速时长，弓箭填 0',
  '突刺最大速度，弓箭填 0',
  '齐射箭矢数量，长矛填 0',
  '齐射扇形扩散角度，长矛填 0',
];

const allyNotes = [
  '兵种唯一标识',
  '兵种显示名称',
  '最大生命值',
  '攻击力',
  '防御力',
  '移动速度',
  '攻击距离',
  '攻击间隔（秒）',
  '死亡后重生时间（秒）',
  '箭矢飞行速度，弓箭手填值，其他填 0',
  '抛物线高度，弓箭手填值，其他填 0',
  '风筝保持距离，弓箭手填值，其他填 0',
  '冲锋速度，骑士填值，其他填 0',
  '冲锋时长，骑士填值，其他填 0',
  '冲锋伤害倍率，骑士填值，其他填 0',
  '冲锋冷却时间，骑士填值，其他填 0',
];

const buildingRows = buildingTable.buildings.map((building) =>
  makeRow(buildingHeaders, {
    buildingId: building.buildingId,
    buildingName: building.buildingName,
    description: building.description,
    buildingType: building.buildingType,
    coinCost: building.coinCost,
    maxHP: building.maxHP,
    atk: building.atk,
    def: building.def,
    attackRange: building.attackRange,
    attackInterval: building.attackInterval,
    arrowSpeed: building.arrowSpeed,
    arcHeight: building.arcHeight,
    aoeRadius: building.aoeRadius,
    dailyYield: building.dailyYield,
    recruitCost: building.recruitCost,
    maxRecruits: building.maxRecruits,
    upgradeIds: Array.isArray(building.upgradeIds) ? building.upgradeIds.join(',') : '',
    branchIcon: building.branchIcon ?? '',
    allyUnitType: building.allyUnitType ?? '',
  }),
);

const monsterRows = monsterTable.monsters.map((monster) =>
  makeRow(monsterHeaders, {
    monsterId: monster.monsterId,
    monsterName: monster.monsterName,
    maxHP: monster.maxHP,
    atk: monster.atk,
    def: monster.def,
    moveSpeed: monster.moveSpeed,
    attackRange: monster.attackRange,
    attackInterval: monster.attackInterval,
    iconLabel: monster.iconLabel,
  }),
);

const waveRows = [
  makeRow(waveHeaders, {
    rowType: 'setting',
    key: 'dailyBaseCoins',
    value: waveTable.dailyBaseCoins,
  }),
  makeRow(waveHeaders, {
    rowType: 'setting',
    key: 'startingCoins',
    value: waveTable.startingCoins,
  }),
];

for (const wave of [...waveTable.waves].sort((a, b) => a.dayNumber - b.dayNumber)) {
  for (const spawnPoint of wave.spawnPoints) {
    for (const action of spawnPoint.actions) {
      waveRows.push(
        makeRow(waveHeaders, {
          rowType: 'spawn',
          dayNumber: wave.dayNumber,
          spawnX: spawnPoint.position.x,
          spawnY: spawnPoint.position.y,
          spawnZ: spawnPoint.position.z,
          monsterId: action.monsterId,
          count: action.count,
          spawnInterval: action.spawnInterval,
        }),
      );
    }
  }
}

const heroRows = [
  makeRow(heroHeaders, { rowType: 'hero', key: 'maxHP', value: heroTable.hero.maxHP }),
  makeRow(heroHeaders, { rowType: 'hero', key: 'moveSpeed', value: heroTable.hero.moveSpeed }),
  makeRow(heroHeaders, { rowType: 'hero', key: 'reviveTime', value: heroTable.hero.reviveTime }),
];

for (const weapon of heroTable.hero.weapons) {
  heroRows.push(
    makeRow(heroHeaders, {
      rowType: 'weapon',
      weaponId: weapon.weaponId,
      weaponName: weapon.weaponName,
      atk: weapon.atk,
      def: weapon.def,
      attackRange: weapon.attackRange,
      attackInterval: weapon.attackInterval,
      arrowSpeed: weapon.arrowSpeed,
      arcHeight: weapon.arcHeight,
    }),
  );

  if (weapon.skill) {
    heroRows.push(
      makeRow(heroHeaders, {
        rowType: 'skill',
        weaponId: weapon.weaponId,
        arrowSpeed: weapon.skill.arrowSpeed,
        arcHeight: weapon.skill.arcHeight,
        skillId: weapon.skill.skillId,
        cooldown: weapon.skill.cooldown,
        damageMultiplier: weapon.skill.damageMultiplier,
        accelTime: weapon.skill.accelTime,
        decelTime: weapon.skill.decelTime,
        maxSpeed: weapon.skill.maxSpeed,
        arrowCount: weapon.skill.arrowCount,
        spreadAngle: weapon.skill.spreadAngle,
      }),
    );
  }
}

const allyRows = allyUnitTable.units.map((unit) =>
  makeRow(allyHeaders, {
    unitType: unit.unitType,
    unitName: unit.unitName,
    maxHP: unit.maxHP,
    atk: unit.atk,
    def: unit.def,
    moveSpeed: unit.moveSpeed,
    attackRange: unit.attackRange,
    attackInterval: unit.attackInterval,
    respawnTime: unit.respawnTime,
    arrowSpeed: unit.arrowSpeed,
    arcHeight: unit.arcHeight,
    kiteDistance: unit.kiteDistance,
    chargeSpeed: unit.chargeSpeed,
    chargeDuration: unit.chargeDuration,
    chargeMultiplier: unit.chargeMultiplier,
    chargeCooldown: unit.chargeCooldown,
  }),
);

const sheetDefs = [
  { name: 'building', headers: buildingHeaders, notes: buildingNotes, rows: buildingRows },
  { name: 'monster', headers: monsterHeaders, notes: monsterNotes, rows: monsterRows },
  { name: 'wave', headers: waveHeaders, notes: waveNotes, rows: waveRows },
  { name: 'hero', headers: heroHeaders, notes: heroNotes, rows: heroRows },
  { name: 'allyUnit', headers: allyHeaders, notes: allyNotes, rows: allyRows },
];

const stagingDir = mkdtempSync(join(tmpdir(), 'thronefall-config-excel-'));
const zipPath = `${outputPath}.zip`;

try {
  writeTextFile(join(stagingDir, '[Content_Types].xml'), buildContentTypes(sheetDefs.length));
  writeTextFile(join(stagingDir, '_rels', '.rels'), buildRootRels());
  writeTextFile(join(stagingDir, 'xl', 'workbook.xml'), buildWorkbookXml(sheetDefs));
  writeTextFile(join(stagingDir, 'xl', '_rels', 'workbook.xml.rels'), buildWorkbookRels(sheetDefs.length));

  sheetDefs.forEach((sheet, index) => {
    const sheetXml = buildSheetXml(sheet.headers, sheet.notes, sheet.rows);
    writeTextFile(join(stagingDir, 'xl', 'worksheets', `sheet${index + 1}.xml`), sheetXml);
  });

  if (existsSync(outputPath)) {
    rmSync(outputPath, { force: true });
  }
  if (existsSync(zipPath)) {
    rmSync(zipPath, { force: true });
  }

  const compressCommand = [
    `$src = Join-Path ${psQuote(stagingDir)} '*'`,
    `Compress-Archive -Path $src -DestinationPath ${psQuote(zipPath)} -Force`,
  ].join('; ');

  execFileSync('powershell.exe', ['-NoProfile', '-Command', compressCommand], {
    stdio: 'inherit',
  });

  renameSync(zipPath, outputPath);

  if (!existsSync(metaPath)) {
    const guid = randomUUID().replace(/-/g, '');
    writeFileSync(
      metaPath,
      `fileFormatVersion: 2\nguid: ${guid}\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`,
      'ascii',
    );
  }

  console.log(`Generated ${outputPath}`);
} finally {
  if (existsSync(zipPath)) {
    rmSync(zipPath, { force: true });
  }
  rmSync(stagingDir, { recursive: true, force: true });
}
